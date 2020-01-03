using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElf.Sdk.CSharp;
using Google.Protobuf;

namespace AElf.Contracts.Referendum
{
    public partial class ReferendumContract
    {
        private bool IsReleaseThresholdReached(ProposalInfo proposal, Organization organization)
        {
            var proposalReleaseThreshold = organization.ProposalReleaseThreshold;
            var enoughVote = proposal.RejectionCount.Add(proposal.AbstentionCount).Add(proposal.ApprovalCount) >=
                             proposalReleaseThreshold.MinimalVoteThreshold;
            if (!enoughVote)
                return false;

            var isRejected = proposal.RejectionCount > proposalReleaseThreshold.MaximalRejectionThreshold;
            if (isRejected)
                return false;

            var isAbstained = proposal.AbstentionCount > proposalReleaseThreshold.MaximalAbstentionThreshold;
            if (isAbstained)
                return false;

            return proposal.ApprovalCount >= proposalReleaseThreshold.MinimalApprovalThreshold;
        }

        private void RequireTokenContractStateSet()
        {
            if (State.TokenContract.Value != null)
                return;
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
        }

        private void LockToken(string symbol, long amount, Hash proposalId, Address lockedAddress)
        {
            Assert(State.LockedTokenAmount[lockedAddress][proposalId] == null, "Already locked.");

            var lockId = Hash.FromTwoHashes(Hash.FromTwoHashes(Hash.FromMessage(proposalId), Context.TransactionId),
                Hash.FromRawBytes(Context.CurrentBlockTime.ToByteArray()));
            RequireTokenContractStateSet();
            State.TokenContract.Lock.Send(new LockInput
            {
                Address = Context.Sender,
                Symbol = symbol,
                Amount = amount,
                LockId = lockId,
                Usage = "Referendum."
            });
            State.LockedTokenAmount[Context.Sender][proposalId] = new Receipt
            {
                Amount = amount,
                LockId = lockId,
                TokenSymbol = symbol
            };
        }

        private void UnlockToken(Hash proposalId, Address lockedAddress)
        {
            RequireTokenContractStateSet();
            var receipt = State.LockedTokenAmount[lockedAddress][proposalId];
            Assert(receipt != null, "Nothing to reclaim.");
            State.TokenContract.Unlock.Send(new UnlockInput
            {
                Amount = receipt.Amount,
                Address = Context.Sender,
                LockId = receipt.LockId,
                Symbol = receipt.TokenSymbol,
                Usage = "Referendum."
            });
            State.LockedTokenAmount[Context.Sender].Remove(proposalId);
        }

        private bool Validate(Organization organization)
        {
            if (string.IsNullOrEmpty(organization.TokenSymbol) || organization.OrganizationAddress == null ||
                organization.OrganizationHash == null || organization.ProposerWhiteList.Empty())
                return false;

            var proposalReleaseThreshold = organization.ProposalReleaseThreshold;
            return proposalReleaseThreshold.MinimalApprovalThreshold <= proposalReleaseThreshold.MinimalVoteThreshold &&
                   proposalReleaseThreshold.MinimalApprovalThreshold > 0 &&
                   proposalReleaseThreshold.MaximalAbstentionThreshold >= 0 &&
                   proposalReleaseThreshold.MaximalRejectionThreshold >= 0;
        }

        private bool Validate(ProposalInfo proposal)
        {
            var validDestinationAddress = proposal.ToAddress != null;
            var validDestinationMethodName = !string.IsNullOrWhiteSpace(proposal.ContractMethodName);
            var validExpiredTime = proposal.ExpiredTime != null && Context.CurrentBlockTime < proposal.ExpiredTime;
            var hasOrganizationAddress = proposal.OrganizationAddress != null;
            return validDestinationAddress && validDestinationMethodName && validExpiredTime & hasOrganizationAddress;
        }

        private ProposalInfo GetValidProposal(Hash proposalId)
        {
            var proposal = State.Proposals[proposalId];
            Assert(proposal != null, "Invalid proposal id.");
            Assert(Validate(proposal), "Invalid proposal.");
            return proposal;
        }

        private long GetAllowance(Address owner, string tokenSymbol)
        {
            RequireTokenContractStateSet();
            var allowance = State.TokenContract.GetAllowance.Call(new GetAllowanceInput
            {
                Owner = owner,
                Spender = Context.Self,
                Symbol = tokenSymbol
            }).Allowance;
            Assert(allowance > 0, "Allowance not enough.");
            return allowance;
        }

        private Hash CreateNewProposal(CreateProposalInput input)
        {
            Hash proposalId = Hash.FromTwoHashes(Hash.FromTwoHashes(Hash.FromMessage(input), Context.TransactionId),
                Hash.FromRawBytes(Context.CurrentBlockTime.ToByteArray()));
            Assert(State.Proposals[proposalId] == null, "Proposal already exists.");
            var proposal = new ProposalInfo
            {
                ContractMethodName = input.ContractMethodName,
                ToAddress = input.ToAddress,
                ExpiredTime = input.ExpiredTime,
                Params = input.Params,
                OrganizationAddress = input.OrganizationAddress,
                Proposer = Context.Sender
            };
            Assert(Validate(proposal), "Invalid proposal.");
            State.Proposals[proposalId] = proposal;
            Context.Fire(new ProposalCreated {ProposalId = proposalId});

            return proposalId;
        }

        private void AssertIsAuthorizedProposer(Address organizationAddress, Address proposer)
        {
            var organization = State.Organisations[organizationAddress];
            Assert(organization != null, "Organization not found.");
            Assert(organization.ProposerWhiteList.Contains(proposer), "Unauthorized to propose.");
        }

        private OrganizationHashAddressPair CalculateOrganizationHashAddressPair(
            CreateOrganizationInput createOrganizationInput)
        {
            var organizationHash = Hash.FromMessage(createOrganizationInput);
            var organizationAddress = Context.ConvertVirtualAddressToContractAddressWithContractHashName(organizationHash);
            return new OrganizationHashAddressPair
            {
                OrganizationAddress = organizationAddress,
                OrganizationHash = organizationHash
            };
        }
    }
}
using System;
using Acs3;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Contracts.ReferendumAuth
{
    public partial class ReferendumAuthContract : ReferendumAuthContractContainer.ReferendumAuthContractBase
    {
        #region View

        public override Organization GetOrganization(Address address)
        {
            var organization = State.Organisations[address];
            Assert(organization != null, "No registered organization.");
            return organization;
        }
        public override ProposalOutput GetProposal(Hash proposalId)
        {
            var proposal = State.Proposals[proposalId];
            Assert(proposal != null, "Proposal not found.");

            var result = new ProposalOutput
            {
                ProposalId = proposalId,
                ContractMethodName = proposal.ContractMethodName,
                ExpiredTime = proposal.ExpiredTime,
                OrganizationAddress = proposal.OrganizationAddress,
                Params = proposal.Params,
                Proposer = proposal.Proposer,
                ToAddress = proposal.ToAddress,
            };

            return result;
        }

        #endregion
        
        public override Empty Initialize(ReferendumAuthContractInitializationInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.Initialized.Value = true;
            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();
            State.TokenContractSystemName.Value = input.TokenContractSystemName;
            return new Empty();
        }
        
        public override Address CreateOrganization(CreateOrganizationInput input)
        {
            var organizationHash = Hash.FromTwoHashes(Hash.FromMessage(Context.Self), Hash.FromMessage(input));
            Address organizationAddress = Context.ConvertVirtualAddressToContractAddress(organizationHash);
            if(State.Organisations[organizationAddress] == null)
            {
                var organization = new Organization
                {
                    ReleaseThreshold = input.ReleaseThreshold,
                    OrganizationAddress = organizationAddress,
                    TokenSymbol = input.TokenSymbol,
                    OrganizationHash = organizationHash
                };
                State.Organisations[organizationAddress] = organization;
            }
            return organizationAddress;
        }

        public override Hash CreateProposal(CreateProposalInput proposal)
        {
            Assert(
                !string.IsNullOrWhiteSpace(proposal.ContractMethodName)
                && proposal.ToAddress != null
                && proposal.OrganizationAddress != null
                && proposal.ExpiredTime != null, "Invalid proposal.");
            var organization = State.Organisations[proposal.OrganizationAddress];
            Assert(organization != null, "No registered organization.");
            DateTime timestamp = proposal.ExpiredTime.ToDateTime();
            Assert(Context.CurrentBlockTime < timestamp, "Expired proposal.");
            Hash hash = Hash.FromMessage(proposal);
            Assert(State.Proposals[hash] == null, "Proposal already exists.");
            State.Proposals[hash] = new ProposalInfo
            {
                ContractMethodName = proposal.ContractMethodName,
                ToAddress = proposal.ToAddress,
                ExpiredTime = proposal.ExpiredTime,
                Params = proposal.Params,
                OrganizationAddress = proposal.OrganizationAddress,
                Proposer = Context.Sender
            };
            return hash;
        }

        public override BoolValue Approve(ApproveInput approval)
        {
            var proposalInfo = State.Proposals[approval.ProposalId];
            Assert(proposalInfo != null, "Proposal not found.");
            DateTime timestamp = proposalInfo.ExpiredTime.ToDateTime();
            if (Context.CurrentBlockTime > timestamp)
            {
                // expired proposal
                //State.Proposals[approval.ProposalId] = null;
                return new BoolValue{Value = false};
            }
            Assert(approval.Quantity > 0, "Invalid vote.");
            var lockedTokenAmount = approval.Quantity;

            Assert(State.LockedTokenAmount[Context.Sender][approval.ProposalId] == null,
                "Cannot approve more than once.");
            var organization = GetOrganization(proposalInfo.OrganizationAddress);
            State.LockedTokenAmount[Context.Sender][approval.ProposalId] = new Receipt
            {
                Amount = lockedTokenAmount,
                LockId = Context.TransactionId,
                TokenSymbol = organization.TokenSymbol
            };
            State.ApprovedTokenAmount[approval.ProposalId] += lockedTokenAmount;

            LockToken(new LockInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = organization.TokenSymbol,
                Amount = lockedTokenAmount,
                LockId = Context.TransactionId,
                Usage = "Referendum."
            });

            if (IsReadyToRelease(approval.ProposalId, organization))
            {
                Context.SendVirtualInline(organization.OrganizationHash, proposalInfo.ToAddress, proposalInfo.ContractMethodName,
                    proposalInfo.Params);
                //State.Proposals[approval.ProposalId] = null;
            }
            return new BoolValue{Value = true};
        }

        public override Empty ReclaimVoteToken(Hash proposalId)
        {
            var voteToken = State.LockedTokenAmount[Context.Sender][proposalId];
            Assert(voteToken != null, "Nothing to reclaim.");
            var proposal = State.Proposals[proposalId];
            Assert(proposal == null ||
                Context.CurrentBlockTime > proposal.ExpiredTime.ToDateTime(), "Unable to reclaim at this time.");
            // State.LockedTokenAmount[Context.Sender][proposalId] = null;
            UnlockToken(new UnlockInput
            {
                Amount = voteToken.Amount,
                From = Context.Sender,
                LockId = voteToken.LockId,
                Symbol = voteToken.TokenSymbol,
                To = Context.Self,
                Usage = "Referendum."
            });
            return new Empty();
        }
    }
}
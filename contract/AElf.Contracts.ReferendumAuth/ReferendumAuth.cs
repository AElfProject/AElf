using System;
using Acs3;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
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
            return State.Organisations[address] ?? new Organization();
        }

        public override ProposalOutput GetProposal(Hash proposalId)
        {
            var proposal = State.Proposals[proposalId];
            if (proposal == null)
            {
                return new ProposalOutput();
            }

            return new ProposalOutput
            {
                ProposalId = proposalId,
                ContractMethodName = proposal.ContractMethodName,
                ExpiredTime = proposal.ExpiredTime,
                OrganizationAddress = proposal.OrganizationAddress,
                Params = proposal.Params,
                Proposer = proposal.Proposer,
                ToAddress = proposal.ToAddress,
            };
        }

        #endregion

        public override Empty Initialize(Empty input)
        {
            return new Empty();
        }

        public override Address CreateOrganization(CreateOrganizationInput input)
        {
            var organizationHash = Hash.FromTwoHashes(Hash.FromMessage(Context.Self), Hash.FromMessage(input));
            var organizationAddress = Context.ConvertVirtualAddressToContractAddress(organizationHash);
            var organization = new Organization
            {
                ReleaseThreshold = input.ReleaseThreshold,
                OrganizationAddress = organizationAddress,
                TokenSymbol = input.TokenSymbol,
                OrganizationHash = organizationHash
            };

            if (State.Organisations[organizationAddress] == null)
            {
                State.Organisations[organizationAddress] = organization;
            }

            return organizationAddress;
        }

        public override Hash CreateProposal(CreateProposalInput input)
        {
            var organization = State.Organisations[input.OrganizationAddress];
            Assert(organization != null, "No registered organization.");

            Hash hash = Hash.FromMessage(input);
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
            Assert(State.Proposals[hash] == null, "Proposal already exists.");
            State.Proposals[hash] = proposal;
            return hash;
        }

        public override BoolValue Approve(ApproveInput input)
        {
            var proposal = GetValidProposal(input.ProposalId);

            Assert(input.Quantity > 0, "Invalid vote.");
            var lockAmount = input.Quantity;

            Assert(State.LockedTokenAmount[Context.Sender][input.ProposalId] == null, "Cannot approve more than once.");
            State.ApprovedTokenAmount[input.ProposalId] =
                State.ApprovedTokenAmount[input.ProposalId].Add(lockAmount);

            var organization = State.Organisations[proposal.OrganizationAddress];
            if (IsReleaseThresholdReached(input.ProposalId, organization))
            {
                Context.SendVirtualInline(
                    organization.OrganizationHash,
                    proposal.ToAddress,
                    proposal.ContractMethodName,
                    proposal.Params);
                //State.Proposals[approval.ProposalId] = null;
            }

            LockToken(new LockInput
            {
                Address = Context.Sender,
                Symbol = organization.TokenSymbol,
                Amount = lockAmount,
                LockId = Context.TransactionId,
                Usage = "Referendum."
            });
            // Register receipt
            State.LockedTokenAmount[Context.Sender][input.ProposalId] = new Receipt
            {
                Amount = lockAmount,
                LockId = Context.TransactionId,
                TokenSymbol = organization.TokenSymbol
            };
            return new BoolValue {Value = true};
        }

        public override Empty ReclaimVoteToken(Hash proposalId)
        {
            var lockReceipt = State.LockedTokenAmount[Context.Sender][proposalId];
            Assert(lockReceipt != null, "Nothing to reclaim.");
            var proposal = State.Proposals[proposalId];
            Assert(proposal == null ||
                   Context.CurrentBlockTime > proposal.ExpiredTime, "Unable to reclaim at this time.");
            // State.LockedTokenAmount[Context.Sender][proposalId] = null;
            UnlockToken(new UnlockInput
            {
                Amount = lockReceipt.Amount,
                Address = Context.Sender,
                LockId = lockReceipt.LockId,
                Symbol = lockReceipt.TokenSymbol,
                Usage = "Referendum."
            });
            return new Empty();
        }
    }
}
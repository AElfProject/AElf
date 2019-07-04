using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.ParliamentAuth
{
    public partial class ParliamentAuthContract
    {
        private List<Address> GetCurrentMinerList()
        {
            ValidateConsensusContract();
            var miner = State.ConsensusContract.GetCurrentMinerList.Call(new Empty());
            var representatives = miner.Pubkeys.Select(publicKey =>
                Address.FromPublicKey(publicKey.ToByteArray())).ToList();
            return representatives;
        }

        private void CheckProposerAuthority(Organization organization)
        {
            // It is a valid proposer if
            // authority check is disable,
            // or sender is in proposer white list,
            // or sender is one of miners.
            if (!organization.ProposerAuthorityRequired)
                return; 
            if (organization.ProposerWhiteList.Any(p => p == Context.Sender))
                return;
            var minerList = GetCurrentMinerList();
            Assert(minerList.Any(m => m == Context.Sender), "Not authorized to propose.");
        }
        
        private bool IsReadyToRelease(ProposalInfo proposal, Organization organization,
            IEnumerable<Address> representatives)
        {
            var validApprovalWeights = proposal.ApprovedRepresentatives.Aggregate(0,
                (weights, address) =>
                    weights + (representatives.FirstOrDefault(r => r.Equals(address)) == null ? 0 : 1));
            return validApprovalWeights * 10000 >= organization.ReleaseThreshold * representatives.Count();
        }
        
        private void ValidateConsensusContract()
        {
            if (State.ConsensusContract.Value != null)
                return;
            State.ConsensusContract.Value = Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
        }

        private bool IsValidRepresentative(IEnumerable<Address> representatives)
        {
            return representatives.Any(r => r.Equals(Context.Sender));
        }

        private Hash GenerateOrganizationVirtualHash(CreateOrganizationInput input)
        {
            return Hash.FromTwoHashes(Hash.FromMessage(Context.Self), Hash.FromMessage(input));
        }
    }
}
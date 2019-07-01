using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.ParliamentAuth
{
    public partial class ParliamentAuthContract
    {
        private List<Address> GetRepresentatives()
        {
            ValidateConsensusContract();
            var miner = State.ConsensusContract.GetCurrentMinerList.Call(new Empty());
            var representatives = miner.Pubkeys.Select(publicKey =>
                Address.FromPublicKey(publicKey.ToByteArray())).ToList();
            return representatives;
        }

        private void CheckProposerAuthority(Address organizationAddress)
        {
            // add some checks if needed. Any one can propose if no checking here.
            // TODO: proposer authority to be checked
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
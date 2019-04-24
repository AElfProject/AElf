using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.ParliamentAuth
{
    public partial class ParliamentAuthContract
    {
        private List<Address> GetRepresentatives()
        {
            ValidateConsensusContract();
            var miner = State.ConsensusContract.GetCurrentMiners.Call(new Empty());
            var representatives = miner.MinerList.PublicKeys.Select(publicKey =>
                Address.FromPublicKey(ByteArrayHelpers.FromHexString(publicKey))).ToList();
            return representatives;
        }

        private void CheckProposerAuthority(Address organizationAddress)
        {
            // add some checks if needed. Any one can propose if no checking here.
            var organization = GetOrganization(organizationAddress);
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
            State.ConsensusContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.ConsensusContractSystemName.Value);
        }

        private bool IsValidRepresentative(IEnumerable<Address> representatives)
        {
            return representatives.Any(r => r.Equals(Context.Sender));
        }

        private Hash GenerateOrganizationVirtualHash(CreateOrganizationInput input)
        {
            return Hash.FromTwoHashes(Hash.FromMessage(Context.Self), Hash.FromMessage(input));
        }

        private int _defaultOrganizationReleaseThreshold = 6666; // 2/3 for default parliament organization
    }
}
using AElf.Contracts.Election;
using AElf.Contracts.treasury;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContractState
    {
        internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
        internal TreasuryContractContainer.TreasuryContractReferenceState TreasuryContract { get; set; }
    }
}
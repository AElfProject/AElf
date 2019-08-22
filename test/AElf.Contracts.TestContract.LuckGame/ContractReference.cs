using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;

namespace AElf.Contracts.TestContract.LuckGame
{
    public partial class LuckGameContractState
    {
        internal AEDPoSContractContainer.AEDPoSContractReferenceState ConsensusContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    }
}
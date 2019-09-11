using Acs4;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        private interface IConsensusCommandProvider
        {
            ConsensusCommand GetConsensusCommand();
        }
    }
}
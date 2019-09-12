using Acs4;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        public class TerminateRoundCommandStrategy : ICommandStrategy
        {
            public ConsensusCommand GetConsensusCommand()
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
using Acs4;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        public class NormalBlockCommandStrategy : ICommandStrategy
        {
            public ConsensusCommand GetConsensusCommand()
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
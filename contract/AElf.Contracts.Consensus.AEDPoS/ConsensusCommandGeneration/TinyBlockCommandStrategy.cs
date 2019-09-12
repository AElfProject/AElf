using Acs4;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        public class TinyBlockCommandStrategy : ICommandStrategy
        {
            public ConsensusCommand GetConsensusCommand()
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
using Acs4;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        public interface ICommandStrategy
        {
            ConsensusCommand GetConsensusCommand();
        }
    }
}
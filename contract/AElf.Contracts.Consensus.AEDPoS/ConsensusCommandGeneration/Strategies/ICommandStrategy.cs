using AElf.Standards.ACS4;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        /// <summary>
        /// To provide consensus command for different consensus behaviours, like
        /// consensus command of blocks in first round,
        /// consensus command of normal block (miner will publish out value and previous in value),
        /// consensus command of tiny block (miner just update his latest mining time)
        /// consensus command of terminating current round.
        /// </summary>
        protected interface ICommandStrategy
        {
            ConsensusCommand GetConsensusCommand();
        }
    }
}
namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusScheduler
    {
        /// <summary>
        /// Update scheduler event.
        /// </summary>
        /// <param name="countingMilliseconds"></param>
        /// <param name="consensusRequestMiningEventData"></param>
        void NewEvent(long countingMilliseconds, ConsensusRequestMiningEventData consensusRequestMiningEventData);

        /// <summary>
        /// Cancel previous event.
        /// </summary>
        void CancelCurrentEvent();
    }
}
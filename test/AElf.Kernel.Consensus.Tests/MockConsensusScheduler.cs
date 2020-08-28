using AElf.Kernel.Consensus.Application;

namespace AElf.Kernel.Consensus
{
    /// <summary>
    /// Mock this for testing method TriggerConsensusAsync in ConsensusService.
    /// </summary>
    public class MockConsensusScheduler : IConsensusScheduler
    {
        private readonly IConsensusTestHelper _consensusTestHelper;

        public MockConsensusScheduler(IConsensusTestHelper consensusTestHelper)
        {
            _consensusTestHelper = consensusTestHelper;
        }

        public void NewEvent(long countingMilliseconds, ConsensusRequestMiningEventData consensusRequestMiningEventData)
        {
            _consensusTestHelper.IsConsensusSchedulerFilled = true;
        }

        public void CancelCurrentEvent()
        {
            _consensusTestHelper.IsConsensusSchedulerFilled = false;
        }
    }
}
using AElf.ContractTestKit;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class ResetBlockTimeProvider : IResetBlockTimeProvider
    {
        public bool Enabled => false;
        public int StepMilliseconds => 0;
    }
}
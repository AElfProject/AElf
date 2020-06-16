using AElf.ContractTestKit;

namespace AElf.Contracts.TokenHolder
{
    public class ResetBlockTimeProvider : IResetBlockTimeProvider
    {
        public bool Enabled => false;
        public int StepMilliseconds => 0;
    }
}
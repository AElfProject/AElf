using AElf.ContractTestBase.ContractTestKit;

namespace AElf.Contracts.MultiToken
{
    public class ResetBlockTimeProvider : IResetBlockTimeProvider
    {
        public bool Enabled => false;
        public int StepMilliseconds => 0;
    }
}
namespace AElf.ContractTestBase.ContractTestKit
{
    public interface IResetBlockTimeProvider
    {
        bool Enabled { get; }
        int StepMilliseconds { get; }
    }

    public class ResetBlockTimeProvider : IResetBlockTimeProvider
    {
        public bool Enabled => true;
        public int StepMilliseconds => 4000;
    }
}
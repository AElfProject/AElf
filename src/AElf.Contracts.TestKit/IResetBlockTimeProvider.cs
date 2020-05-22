namespace AElf.Contracts.TestKit
{
    public interface IResetBlockTimeProvider
    {
        int StepMilliseconds { get; }
    }

    public class ResetBlockTimeProvider : IResetBlockTimeProvider
    {
        public int StepMilliseconds => 4000;
    }
}
using AElf.Contracts.TestKit;

namespace AElf.Contracts.TokenHolder
{
    public class ResetBlockTimeProvider : IResetBlockTimeProvider
    {
        public bool Enabled => false;
        public int StepMilliseconds => 0;
    }
}
namespace AElf.Kernel.EventMessages
{
    public sealed class CatchingUpAfterRollback
    {
        public bool IsCatchingUp { get; }

        public CatchingUpAfterRollback(bool isCatchingUp)
        {
            IsCatchingUp = isCatchingUp;
        }
    }
}
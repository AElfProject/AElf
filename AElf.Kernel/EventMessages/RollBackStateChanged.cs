namespace AElf.Kernel.EventMessages
{
    public sealed class RollBackStateChanged
    {
        public bool DoingRollback { get; }

        public RollBackStateChanged(bool doingRollback)
        {
            DoingRollback = doingRollback;
        }
    }
}
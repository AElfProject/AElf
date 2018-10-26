namespace AElf.Kernel.EventMessages
{
    public class RollBackStateChanged
    {
        public bool DoingRollback { get; }

        public RollBackStateChanged(bool doingRollback)
        {
            DoingRollback = doingRollback;
        }
    }
}
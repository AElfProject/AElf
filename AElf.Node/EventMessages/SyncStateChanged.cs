namespace AElf.Node.EventMessages
{
    public class SyncStateChanged
    {
        public bool IsSyncing { get; private set; }

        public SyncStateChanged(bool value)
        {
            IsSyncing = value;
        }
    }
}
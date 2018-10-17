namespace AElf.Node.EventMessages
{
    public class SyncStateChanged
    {
        public bool IsSyncing { get; }

        public SyncStateChanged(bool value)
        {
            IsSyncing = value;
        }
    }
}
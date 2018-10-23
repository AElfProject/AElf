namespace AElf.ChainController.EventMessages
{
    public sealed class SyncStateChanged
    {
        public bool IsSyncing { get; }

        public SyncStateChanged(bool value)
        {
            IsSyncing = value;
        }
    }
}
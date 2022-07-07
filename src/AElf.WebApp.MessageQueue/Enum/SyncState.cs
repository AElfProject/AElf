namespace AElf.WebApp.MessageQueue.Enum;

public enum SyncState
{
    Stopping,
    Stopped,
    Prepared,
    SyncPrepared,
    SyncRunning,
    AsyncRunning
}
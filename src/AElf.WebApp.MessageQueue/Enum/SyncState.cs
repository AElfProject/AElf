namespace AElf.WebApp.MessageQueue.Enum;

public enum SyncState
{
    Stopped,
    Prepared,
    SyncPrepared,
    SyncRunning,
    AsyncRunning
}
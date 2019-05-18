namespace AElf.Kernel.Node.Infrastructure
{
    public interface INodeSyncStateProvider
    {
        bool IsNodeSyncing();
        bool SetSyncing(bool value);
    }
}
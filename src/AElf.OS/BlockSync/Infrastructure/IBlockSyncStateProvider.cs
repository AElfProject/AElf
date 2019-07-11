using Google.Protobuf.WellKnownTypes;

namespace AElf.OS.BlockSync.Infrastructure
{
    public interface IBlockSyncStateProvider
    {
        Timestamp GetEnqueueTime(string queueName);

        void SetEnqueueTime(string queueName, Timestamp enqueueTime);
    }
}
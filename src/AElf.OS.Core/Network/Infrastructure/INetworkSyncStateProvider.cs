using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    public interface INetworkSyncStateProvider
    {
        Timestamp TimestampForBlockSyncJobEnqueue { get; set; }
    }

    public class NetworkSyncStateProvider : INetworkSyncStateProvider, ISingletonDependency
    {
        public Timestamp TimestampForBlockSyncJobEnqueue { get; set; }
    }
}
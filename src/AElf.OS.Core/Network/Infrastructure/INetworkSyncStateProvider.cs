using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    public interface INetworkSyncStateProvider
    {
        Timestamp BlockSyncJobEnqueueTime { get; set; }
    }

    public class NetworkSyncStateProvider : INetworkSyncStateProvider, ISingletonDependency
    {
        public Timestamp BlockSyncJobEnqueueTime { get; set; }
    }
}
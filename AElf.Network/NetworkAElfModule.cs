using AElf.Configuration.Config.Network;
using AElf.Modularity;
using AElf.Network.Connection;
using AElf.Network.Peers;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Network
{
    public class NetworkAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IConnectionListener, ConnectionListener>();
            context.Services.AddSingleton<IPeerManager, PeerManager>();

            PeerDataStore peerDb = new PeerDataStore(NetworkConfig.Instance.PeersDbPath);

            context.Services.AddSingleton<IPeerDatabase>(peerDb);
        }

    }
}
using AElf.Configuration.Config.Network;
using AElf.Network.Connection;
using AElf.Network.Peers;
using Autofac;

namespace AElf.Network
{
    public class NetworkAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PeerManager>().As<IPeerManager>().SingleInstance();

            builder.RegisterType<NetworkManager>().As<INetworkManager>().SingleInstance();
            builder.RegisterType<ConnectionListener>().As<IConnectionListener>();

            PeerDataStore peerDb = new PeerDataStore(NetworkConfig.Instance.PeersDbPath);
            builder.RegisterInstance(peerDb).As<IPeerDatabase>();

        }
    }
}
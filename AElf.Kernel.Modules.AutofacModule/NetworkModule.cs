using AElf.Network;
using AElf.Network.Config;
using AElf.Network.Data;
using AElf.Network.Peers;
using Autofac;
 
 namespace AElf.Kernel.Modules.AutofacModule
 {
     public class NetworkModule : Module
     {
         public IAElfNetworkConfig NetConfig { get; }
         public bool IsMiner { get; }

         public NetworkModule(IAElfNetworkConfig netConfig, bool isMiner)
         {
             NetConfig = netConfig ?? new AElfNetworkConfig();
             IsMiner = isMiner;
         }

         protected override void Load(ContainerBuilder builder)
         {
             builder.RegisterInstance(NetConfig).As<IAElfNetworkConfig>();
             
             //builder.RegisterType<AElfTcpServer>().As<IAElfServer>();
             
             /*if(IsMiner)
                 builder.RegisterType<BootnodePeerManager>().As<IPeerManager>();
             else
                 builder.RegisterType<PeerManager>().As<IPeerManager>();*/
             
             builder.RegisterType<NetworkManager>().As<IPeerManager>();

             PeerDataStore peerDb = new PeerDataStore(NetConfig.PeersDbPath);
             builder.RegisterInstance(peerDb).As<IPeerDatabase>();

             //NodeData nd = NodeData.FromString(NetConfig.Host + ":" + NetConfig.Port);
             
             //NodeDialer dialer = new NodeDialer(NetConfig.Port);
             //builder.RegisterInstance(dialer).As<INodeDialer>();

         }
     }
 }
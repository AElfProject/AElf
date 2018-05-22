using AElf.Kernel.Node.Network;
using AElf.Kernel.Node.Network.Config;
using AElf.Kernel.Node.Network.Peers;
using Autofac;
 
 namespace AElf.Kernel.Modules.AutofacModule
 {
     public class NetworkModule : Module
     {
         public IAElfServerConfig ServerConfig { get; }
         public IAElfNetworkConfig NetConfig { get; }

         public NetworkModule(IAElfServerConfig serverConfig, IAElfNetworkConfig netConfig)
         {
             ServerConfig = serverConfig ?? new TcpServerConfig();
             NetConfig = netConfig ?? new AElfNetworkConfig();
         }

         protected override void Load(ContainerBuilder builder)
         {
             builder.RegisterInstance(NetConfig).As<IAElfNetworkConfig>();
             builder.RegisterInstance(ServerConfig).As<IAElfServerConfig>();
             
             builder.RegisterType<AElfTcpServer>().As<IAElfServer>();
             builder.RegisterType<PeerManager>().As<IPeerManager>();
             builder.RegisterType<PeerDatabase>().As<IPeerDatabase>();
         }
     }
 }
using AElf.Kernel.Node.Network;
using AElf.Kernel.Node.Network.Config;
using AElf.Kernel.Node.Network.Peers;
using Autofac;
 
 namespace AElf.Kernel.Modules.AutofacModule
 {
     public class NetworkModule : Module
     {
         public IAElfNetworkConfig NetConfig { get; }

         public NetworkModule(IAElfNetworkConfig netConfig)
         {
             NetConfig = netConfig ?? new AElfNetworkConfig();
         }

         protected override void Load(ContainerBuilder builder)
         {
             builder.RegisterInstance(NetConfig).As<IAElfNetworkConfig>();
             
             builder.RegisterType<AElfTcpServer>().As<IAElfServer>();
             builder.RegisterType<PeerManager>().As<IPeerManager>();
             builder.RegisterType<PeerDataStore>().As<IPeerDatabase>();
         }
     }
 }
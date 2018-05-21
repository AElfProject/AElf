using AElf.Kernel.Node.Network;
using AElf.Kernel.Node.Network.Config;
using Autofac;
 
 namespace AElf.Kernel.Modules.AutofacModule
 {
     public class NetworkModule : Module
     {
         public IAElfServerConfig ServerConfig { get; }
         public IAElfNetworkConfig NetConfig { get; }

         public NetworkModule(IAElfServerConfig serverConfig, IAElfNetworkConfig netConfig)
         {
             ServerConfig = serverConfig;
             NetConfig = netConfig;
         }

         protected override void Load(ContainerBuilder builder)
         {
             builder.RegisterType<AElfNetworkConfig>().As<IAElfNetworkConfig>();
             builder.RegisterType<AElfTcpServer>().As<IAElfServerConfig>();
             
             builder.RegisterType<AElfTcpServer>().As<IAElfServer>();
         }
     }
 }
using AElf.Kernel.Node.Network;
using Autofac;
 
 namespace AElf.Kernel.Modules.AutofacModule
 {
     public class NetworkModule : Module
     {
         public IAElfServerConfig ServerConfig { get; set; }

         public NetworkModule(IAElfServerConfig serverConfig)
         {
             ServerConfig = serverConfig;
         }

         protected override void Load(ContainerBuilder builder)
         {
             builder.RegisterType<AElfTcpServer>().As<IAElfServer>();
         }
     }
 }
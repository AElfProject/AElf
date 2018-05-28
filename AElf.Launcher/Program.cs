using System;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Kernel.Node;
using AElf.Kernel.Node.Network.Config;
using AElf.Kernel.TxMemPool;
using Autofac;
using IContainer = Autofac.IContainer;

namespace AElf.Launcher
{
    class Program
    {
        static void Main(string[] args)
        {
            // Parse options
            ConfigParser confParser = new ConfigParser();
            bool parsed = confParser.Parse(args);

            if (!parsed)
                return;
            
            ITxPoolConfig txPoolConf = confParser.TxPoolConfig;
            IAElfNetworkConfig netConf = confParser.NetConfig;
            
            // Setup ioc 
            IContainer container = SetupIocContainer(txPoolConf, netConf);

            if (container == null)
            {
                Console.WriteLine("IoC setup failed");
                return;
            }

            using(var scope = container.BeginLifetimeScope())
            {
                IAElfNode node = scope.Resolve<IAElfNode>();
                
                // Start the system
                node.Start(confParser.Rpc);

                Console.ReadLine();
            }
        }

        private static IContainer SetupIocContainer(ITxPoolConfig txPoolConf, IAElfNetworkConfig netConf)
        {
            var builder = new ContainerBuilder();
            
            // Register everything
            builder.RegisterModule(new MainModule()); // todo : eventually we won't need this
            
            // Module registrations
            builder.RegisterModule(new TxPoolServiceModule(txPoolConf));
            builder.RegisterModule(new TransactionManagerModule());
            builder.RegisterModule(new LoggerModule());
            builder.RegisterModule(new DatabaseModule());
            builder.RegisterModule(new NetworkModule(netConf));
            builder.RegisterModule(new MainChainNodeModule());
            builder.RegisterModule(new RpcServerModule());

            IContainer container = null;
            
            try
            {
                container = builder.Build();
            }
            catch (Exception e)
            {
                return null;
            }

            return container;
        }
    }
}
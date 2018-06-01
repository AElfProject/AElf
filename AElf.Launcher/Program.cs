using System;
using AElf.Database;
using AElf.Database.Config;
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
            IDatabaseConfig databaseConf = confParser.DatabaseConfig;
            
            // Setup ioc 
            IContainer container = SetupIocContainer(txPoolConf, netConf, databaseConf);

            if (container == null)
            {
                Console.WriteLine("IoC setup failed");
                return;
            }

            if (!CheckDBConnect(container))
            {
                Console.WriteLine("Database connection failed");
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

        private static IContainer SetupIocContainer(ITxPoolConfig txPoolConf, IAElfNetworkConfig netConf,IDatabaseConfig databaseConf)
        {
            var builder = new ContainerBuilder();
            
            // Register everything
            builder.RegisterModule(new MainModule()); // todo : eventually we won't need this
            
            // Module registrations
            builder.RegisterModule(new TxPoolServiceModule(txPoolConf));
            builder.RegisterModule(new TransactionManagerModule());
            builder.RegisterModule(new LoggerModule());
            builder.RegisterModule(new DatabaseModule(databaseConf));
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

        private static bool CheckDBConnect(IContainer container)
        {
            var db = container.Resolve<IKeyValueDatabase>();
            return db.IsConnected();
        }
    }
}
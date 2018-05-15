using System;
using AElf.Kernel;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Kernel.Node;
using AElf.Kernel.Node.RPC;
using AElf.Kernel.TxMemPool;
using Autofac;

namespace AElf.Launcher
{
    class Program
    {
        static void Main(string[] args)
        {
            // Parse options
            TxPoolConfig tc = new TxPoolConfig();
            
            // Setup ioc 
            IContainer container = SetupIocContainer(tc);

            if (container == null)
            {
                Console.WriteLine("IoC setup failed");
                return;
            }

            using(var scope = container.BeginLifetimeScope())
            {
                IAElfNode node = scope.Resolve<IAElfNode>();
                
                // Start the system
                node.Start();

                Console.ReadLine();
            }
        }

        private static IContainer SetupIocContainer(ITxPoolConfig txPoolConf)
        {
            var builder = new ContainerBuilder();
            
            // Register everything
            builder.RegisterModule(new MainModule()); // todo : eventually we won't need this
            
            // Module registrations
            builder.RegisterModule(new TxPoolServiceModule(txPoolConf));
            builder.RegisterModule(new TransactionManagerModule());
            builder.RegisterModule(new LoggerModule());
            builder.RegisterModule(new DatabaseModule());

            // Node registration
            builder.RegisterType<MainChainNode>().As<IAElfNode>();
            builder.RegisterType<RpcServer>().As<IRpcServer>();

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
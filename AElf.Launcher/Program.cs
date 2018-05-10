using System;
using AElf.Kernel;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Kernel.Node;
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

            using(var scope = container.BeginLifetimeScope())
            {
                IAElfNode node = scope.Resolve<IAElfNode>();
                node.Start();

                Console.ReadLine();
            }
        }

        private static IContainer SetupIocContainer(ITxPoolConfig txPoolConf)
        {
            var builder = new ContainerBuilder();
            
            // Registrations
            builder.RegisterModule(new MainModule());
            builder.RegisterModule(new TxPoolServiceModule(txPoolConf));
            builder.RegisterModule(new TransactionManagerModule());
            
            // Node registration
            builder.RegisterType<MainChainNode>().As<IAElfNode>();

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
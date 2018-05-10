using System;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Kernel.TxMemPool;
using Autofac;
using Org.BouncyCastle.Bcpg;

namespace AElf.Launcher
{
    class Program
    {
        private AElfNode _aelf;  
        
        static void Main(string[] args)
        {
            // Parse options
            TxPoolConfig tc = new TxPoolConfig();
            tc.EntryThreshold = 666;
            
            // Setup ioc 
            IContainer container = SetupIocContainer(tc);

            using(var scope = container.BeginLifetimeScope())
            {
                ITxPool pool = scope.Resolve<ITxPool>();
                Console.WriteLine(pool.EntryThreshold);

                Console.ReadLine();
            }
        }

        private static IContainer SetupIocContainer(ITxPoolConfig txPoolConf)
        {
            var builder = new ContainerBuilder();
            
            // Registrations
            builder.RegisterModule(new MainModule());
            builder.RegisterModule(new TxPoolServiceModule(txPoolConf));
            builder.RegisterModule(new TxPoolServiceModule(txPoolConf));

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
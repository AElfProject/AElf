using AElf.Modularity;
using AElf.Synchronization.BlockExecution;
using AElf.Synchronization.BlockSynchronization;
using Autofac;
using Volo.Abp.Modularity;

namespace AElf.Synchronization
{
    public class SyncAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {

            var services = context.Services;
            
            
                
            builder.RegisterType<BlockSynchronizer>().As<IBlockSynchronizer>().SingleInstance();
            builder.RegisterType<BlockExecutor>().As<IBlockExecutor>().SingleInstance();
            builder.RegisterType<BlockHeaderValidator>().As<IBlockHeaderValidator>().SingleInstance();
        }

        public void Init(ContainerBuilder builder)
        {
            
            
            
            builder.RegisterModule(new SyncAutofacModule());
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}
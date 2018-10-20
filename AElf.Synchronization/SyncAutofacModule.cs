using AElf.ChainController;
using AElf.Synchronization.BlockExecution;
using AElf.Synchronization.BlockSynchronization;
using Autofac;

namespace AElf.Synchronization
{
    public class SyncAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BlockSynchronizer>().As<IBlockSynchronizer>().SingleInstance();
            builder.RegisterType<BlockExecutor>().As<IBlockExecutor>().SingleInstance();
            
        }
    }
}
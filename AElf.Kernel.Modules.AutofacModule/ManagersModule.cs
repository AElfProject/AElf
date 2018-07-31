using AElf.ChainController;
using AElf.Kernel.Managers;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class ManagersModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BlockManager>().As<IBlockManager>();
            builder.RegisterType<ChainManager>().As<IChainManager>();
            builder.RegisterType<SmartContractManager>().As<ISmartContractManager>();
            builder.RegisterType<TransactionManager>().As<ITransactionManager>();
            builder.RegisterType<TransactionResultManager>().As<ITransactionResultManager>();
        }
    }
}
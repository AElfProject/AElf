using System;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Storages;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class Module: Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var assembly = typeof(AElf.Kernel.IAccount).Assembly;
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();

            builder.RegisterGeneric(typeof(Serializer<>)).As(typeof(ISerializer<>));
            builder.RegisterType(typeof(SmartContractZero)).As(typeof(ISmartContractZero));
            builder.RegisterType(typeof(SmartContractRunnerFactory)).As(typeof(ISmartContractRunnerFactory)).SingleInstance();
            builder.RegisterType(typeof(ChainManager)).As(typeof(IChainManager)).SingleInstance();
            builder.RegisterType(typeof(TransactionManager)).As(typeof(ITransactionManager)).SingleInstance();
            builder.RegisterType(typeof(WorldStateManager)).As(typeof(IWorldStateManager)).SingleInstance();
            builder.RegisterType(typeof(BlockManager)).As(typeof(IBlockManager)).SingleInstance();

            builder.RegisterType(typeof(ChainStore)).As(typeof(IChainStore)).SingleInstance();            
            builder.RegisterType(typeof(TransactionStore)).As(typeof(ITransactionStore)).SingleInstance();            
            builder.RegisterType(typeof(BlockBodyStore)).As(typeof(IBlockBodyStore)).SingleInstance();
            builder.RegisterType(typeof(BlockHeaderStore)).As(typeof(IBlockHeaderStore)).SingleInstance();
            builder.RegisterType(typeof(PointerStore)).As(typeof(IPointerStore)).SingleInstance();
            builder.RegisterType(typeof(WorldStateStore)).As(typeof(IWorldStateStore)).SingleInstance();
            builder.RegisterType(typeof(ChangesStore)).As(typeof(IChangesStore)).SingleInstance();

            builder.RegisterType(typeof(KeyValueDatabase)).As(typeof(IKeyValueDatabase)).SingleInstance();

            builder.RegisterType(typeof(ChainCreationService)).As(typeof(IChainCreationService)).SingleInstance();
            builder.RegisterType(typeof(AccountContextService)).As(typeof(IAccountContextService)).SingleInstance();
            builder.RegisterType(typeof(ChainContextService)).As(typeof(IChainContextService)).SingleInstance();
            
            builder.RegisterType(typeof(AccountDataProvider)).As(typeof(IAccountDataProvider))
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            builder.RegisterType(typeof(AccountDataContext)).As(typeof(IAccountDataContext))
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            
            builder.RegisterType(typeof(Hash)).As(typeof(IHash));

            base.Load(builder);
        }
    }
}
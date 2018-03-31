using System;
using System.Reflection;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Storages;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class Module: Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var dataAccess = Assembly.GetExecutingAssembly();
            
            //builder.RegisterAssemblyTypes(dataAccess).Where(t=> t.Name.EndsWith("Service")|| t.Name.EndsWith("Manager")).AsImplementedInterfaces();

            builder.RegisterGeneric(typeof(Serializer<>)).As(typeof(ISerializer<>));
            builder.RegisterType(typeof(SmartContractZero)).As(typeof(ISmartContractZero)).SingleInstance();
            builder.RegisterType(typeof(SmartContractRunnerFactory)).As(typeof(ISmartContractRunnerFactory)).SingleInstance();
            builder.RegisterType(typeof(ChainManager)).As(typeof(IChainManager)).SingleInstance();
            builder.RegisterType(typeof(TransactionManager)).As(typeof(ITransactionManager)).SingleInstance();
            builder.RegisterType(typeof(WorldStateManager)).As(typeof(IWorldStateManager)).SingleInstance();
            builder.RegisterType(typeof(SmartContractManager)).As(typeof(ISmartContractManager)).SingleInstance();

            builder.RegisterType(typeof(ChainStore)).As(typeof(IChainStore)).SingleInstance();            
            builder.RegisterType(typeof(ChainBlockRelationStore)).As(typeof(IChainBlockRelationStore)).SingleInstance();
            builder.RegisterType(typeof(KeyValueDatabase)).As(typeof(IKeyValueDatabase)).SingleInstance();
            builder.RegisterType(typeof(TransactionStore)).As(typeof(ITransactionStore)).SingleInstance();            

            builder.RegisterType(typeof(ChainCreationService)).As(typeof(IChainCreationService)).SingleInstance();
            builder.RegisterType(typeof(AccountContextService)).As(typeof(IAccountContextService)).SingleInstance();
            builder.RegisterType(typeof(ChainContextService)).As(typeof(IChainContextService)).SingleInstance();
            
            builder.RegisterType(typeof(AccountDataProvider)).As(typeof(IAccountDataProvider))
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            builder.RegisterType(typeof(AccountDataContext)).As(typeof(IAccountDataContext))
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
            
            builder.RegisterType(typeof(Hash)).As(typeof(IHash));
            builder.RegisterType(typeof(CSharpSmartContractRunner)).SingleInstance();

            base.Load(builder);
        }
    }
}
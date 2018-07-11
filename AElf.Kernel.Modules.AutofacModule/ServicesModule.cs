
using Autofac;
using AElf.Services;
using AElf.SmartContract;
using AElf.SmartContract.Metadata;
using AElf.Execution;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class ServicesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ChainCreationService>().As<IChainCreationService>();
            builder.RegisterType<ChainContextService>().As<IChainContextService>();
            builder.RegisterType<TransactionResultService>().As<ITransactionResultService>();
            builder.RegisterType<SmartContractService>().As<ISmartContractService>();
            builder.RegisterType<AccountContextService>().As<IAccountContextService>().SingleInstance();
            builder.RegisterType<ConcurrencyExecutingService>().As<IConcurrencyExecutingService>();
            builder.RegisterType<FunctionMetadataService>().As<IFunctionMetadataService>();
        }
    }
}
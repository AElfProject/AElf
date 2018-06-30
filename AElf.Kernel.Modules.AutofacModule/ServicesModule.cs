using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Services;
using Autofac;

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
            builder.RegisterType<AccountContextService>().As<IAccountContextService>();
            builder.RegisterType<ConcurrencyExecutingService>().As<IConcurrencyExecutingService>();
            builder.RegisterType<FunctionMetadataService>().As<IFunctionMetadataService>();
        }
    }
}
using Autofac;
using AElf.ChainController;
using AElf.SmartContract;
using AElf.SmartContract.Metadata;

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
//            builder.RegisterType<ConcurrencyExecutingService>().As<IExecutingService>();
            builder.RegisterType<FunctionMetadataService>().As<IFunctionMetadataService>();
            builder.RegisterType<ChainService>().As<IChainService>();
        }
    }
}
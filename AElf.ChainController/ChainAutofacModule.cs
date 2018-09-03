using AElf.ChainController.TxMemPool;
using AElf.SmartContract;
using AElf.SmartContract.Metadata;
using Autofac;

namespace AElf.ChainController
{
    public class ChainAutofacModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var assembly = typeof(BlockVaildationService).Assembly;
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
            builder.RegisterType<ContractTxPool>().As<IContractTxPool>().SingleInstance();
            builder.RegisterType<TxPoolService>().As<ITxPoolService>().SingleInstance();
            builder.RegisterType<ChainCreationService>().As<IChainCreationService>();
            builder.RegisterType<ChainContextService>().As<IChainContextService>();
            builder.RegisterType<TransactionResultService>().As<ITransactionResultService>();
            builder.RegisterType<AccountContextService>().As<IAccountContextService>().SingleInstance();
            builder.RegisterType<ChainService>().As<IChainService>();
        }
    }
}
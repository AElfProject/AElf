using AElf.CrossChain.Indexing.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1.FreeFeeTransactions;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Kernel.Txn.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    public class CrossChainAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IBlockExtraDataProvider, CrossChainBlockExtraDataProvider>();
            context.Services.AddTransient<ISystemTransactionGenerator, CrossChainTransactionGenerator>();
            context.Services.AddTransient<IBlockValidationProvider, CrossChainValidationProvider>();
            context.Services.AddSingleton<ITransactionValidationProvider, TxHubEntryPermissionValidationProvider>();
            var crossChainConfiguration = context.Services.GetConfiguration()
                .GetSection(CrossChainConstants.CrossChainExtraDataNamePrefix);
            Configure<CrossChainConfigOptions>(crossChainConfiguration);

            context.Services.AddSingleton<IChargeFeeStrategy, CrossChainContractChargeFeeStrategy>();
            context.Services
                .AddSingleton<IBestChainFoundLogEventHandler, CrossChainIndexingDataProposedLogEventHandler>();
        }
    }
}
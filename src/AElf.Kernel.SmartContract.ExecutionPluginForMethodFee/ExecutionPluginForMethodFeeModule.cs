using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.FreeFeeTransactions;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class ExecutionPluginForMethodFeeModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<ISystemTransactionGenerator, ClaimFeeTransactionGenerator>();
            context.Services.AddSingleton<IChargeFeeStrategy, TokenContractChargeFeeStrategy>();
            context.Services
                .AddSingleton<IBlockAcceptedLogEventProcessor, SymbolListToPayTxFeeUpdatedLogEventProcessor>();
        }
    }
}
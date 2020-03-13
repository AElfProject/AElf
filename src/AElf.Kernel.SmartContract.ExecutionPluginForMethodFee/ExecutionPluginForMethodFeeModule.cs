using AElf.Kernel.FeeCalculation;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    [DependsOn(typeof(SmartContractAElfModule),
        typeof(FeeCalculationModule))]
    public class ExecutionPluginForMethodFeeModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<ISystemTransactionGenerator, ClaimFeeTransactionGenerator>();
            context.Services.AddTransient<ISystemTransactionRecognizer, ClaimFeeTransactionRecognizer>();
            context.Services.AddTransient<ITransactionValidationProvider, TxHubEntryPermissionValidationProvider>();
            context.Services.AddTransient<ITransactionValidationProvider, MethodFeeAffordableValidationProvider>();
            context.Services.AddTransient<ITransactionValidationProvider, TransactionMethodNameValidationProvider>();
            context.Services.AddSingleton<IChargeFeeStrategy, TokenContractChargeFeeStrategy>();
            context.Services
                .AddSingleton<IBlockAcceptedLogEventProcessor, SymbolListToPayTxFeeUpdatedLogEventProcessor>();
        }
    }
}
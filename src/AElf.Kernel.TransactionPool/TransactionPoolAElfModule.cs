using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Kernel.Txn.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.TransactionPool
{
    [DependsOn(typeof(CoreKernelAElfModule))]
    public class TransactionPoolAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            // Validate signature and tx size.
            services.AddSingleton<ITransactionValidationProvider, BasicTransactionValidationProvider>();
            // Validate existence of target contract.
            services.AddSingleton<ITransactionValidationProvider, TransactionToAddressValidationProvider>();
            // Validate proposed method is allowed.
            services.AddSingleton<ITransactionValidationProvider, TransactionMethodNameValidationProvider>();
            services.AddSingleton<ITransactionValidationProvider, TxHubEntryPermissionValidationProvider>();
            // Validate sender's balance is not 0.
            services.AddSingleton<ITransactionValidationProvider, TransactionFromAddressBalanceValidationProvider>();

            services.AddSingleton<ITransactionReadOnlyExecutionService, TransactionReadOnlyExecutionService>();
            var configuration = context.Services.GetConfiguration();
            Configure<TransactionOptions>(configuration.GetSection("Transaction"));
        }
    }
}
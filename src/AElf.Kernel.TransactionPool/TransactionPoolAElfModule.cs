using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
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
            services.AddSingleton<ITransactionValidationProvider, TransactionExecutionValidationProvider>();
            services.AddSingleton<ITransactionValidationProvider, TransactionMethodValidationProvider>();
            
            services.AddSingleton<ITransactionReadOnlyExecutionService, TransactionReadOnlyExecutionService>();
            var configuration = context.Services.GetConfiguration();
            Configure<TransactionOptions>(configuration.GetSection("Transaction"));
        }
    }
}
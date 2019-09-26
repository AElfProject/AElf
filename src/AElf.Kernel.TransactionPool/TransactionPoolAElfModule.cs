using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Application;
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
            services.AddSingleton<ITransactionValidationProvider, BasicTransactionValidationProvider>();
            
            // TODO: Temporarily remove the transaction contract address verification
            // Switching branches depends on block validation passing and attaching successfully,
            // but validation depends on the execution of the block where the deployment contract is located,
            // so it causes a deadlock.
            //services.AddSingleton<ITransactionValidationProvider, TransactionToAddressValidationProvider>();
            
            services.AddSingleton<ITransactionValidationProvider, TransactionFromAddressBalanceValidationProvider>();
            services.AddSingleton<ITransactionReadOnlyExecutionService, TransactionReadOnlyExecutionService>();

            var configuration = context.Services.GetConfiguration();
            Configure<TransactionOptions>(configuration.GetSection("Transaction"));
        }
    }
}
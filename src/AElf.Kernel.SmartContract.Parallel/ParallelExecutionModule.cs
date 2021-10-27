using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Parallel.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.Parallel
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class ParallelExecutionModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IBlockExecutingService, BlockParallelExecutingService>();
            context.Services
                .AddSingleton<IParallelTransactionExecutingService, LocalParallelTransactionExecutingService>();
            context.Services.AddSingleton<ITransactionExecutingService, LocalParallelTransactionExecutingService>();
        }
    }
}
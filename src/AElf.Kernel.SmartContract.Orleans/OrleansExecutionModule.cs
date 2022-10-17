using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Orleans.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.Orleans;

[DependsOn(typeof(SmartContractAElfModule))]
public class OrleansExecutionModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IBlockExecutingService, BlockOrleansExecutingService>();
        context.Services
            .AddSingleton<IOrleansTransactionExecutingService, OrleansTransactionExecutingService>();
        context.Services.AddSingleton<ITransactionExecutingService, OrleansTransactionExecutingService>();
        context.Services
            .AddSingleton<IOrleansTransactionExecutingClientService, OrleansTransactionExecutingClientService>();
    }
}
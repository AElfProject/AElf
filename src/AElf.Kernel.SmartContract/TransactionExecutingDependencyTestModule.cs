using System.Runtime.CompilerServices;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

[assembly: InternalsVisibleTo("AElf.Kernel.SmartContract.Tests")]
namespace AElf.Kernel.SmartContract
{
    [DependsOn(typeof(CoreKernelAElfModule))]
    public class TransactionExecutingDependencyTestModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ILocalParallelTransactionExecutingService, LocalTransactionExecutingService>();
        }
    }
}
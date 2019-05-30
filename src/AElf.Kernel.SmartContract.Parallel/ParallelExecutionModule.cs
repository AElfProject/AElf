using System.Linq;
using AElf.Kernel.SmartContract.Application;
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
            context.Services.AddAssemblyOf<ParallelExecutionModule>();
            var svc = context.Services.Where(s => s.ServiceType == typeof(ITransactionExecutingService)).ToList();
            var svc1 = context.Services
                .Where(s => s.ImplementationType == typeof(LocalParallelTransactionExecutingService)).ToList();
        }
    }
}
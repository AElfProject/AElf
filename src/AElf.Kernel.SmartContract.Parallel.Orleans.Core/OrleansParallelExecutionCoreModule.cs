using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract.Parallel.Orleans
{
    [DependsOn(typeof(ParallelExecutionModule))]
    public class OrleansParallelExecutionCoreModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}
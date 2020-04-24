using AElf.Kernel.FeeCalculation.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForCallThreshold
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class ExecutionPluginForCallThresholdModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}
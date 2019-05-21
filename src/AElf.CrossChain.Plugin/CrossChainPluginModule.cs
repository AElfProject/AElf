using AElf.Kernel.Node.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Plugin
{
    [DependsOn(typeof(CrossChainAElfModule))]
    public class CrossChainPluginModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}
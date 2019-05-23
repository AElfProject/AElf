using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.CrossChain.Communication
{
    [DependsOn(typeof(CrossChainAElfModule))]
    public class CrossChainPluginModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}
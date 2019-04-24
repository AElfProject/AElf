using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf
{
    public class ConsensusModule: AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<ConsensusModule>();
        }
    }
}
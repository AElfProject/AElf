using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Modularity
{
    public abstract class AElfModule : AbpModule
    {
    }

    public abstract class AElfModule<TSelf> : AElfModule
        where TSelf : AElfModule<TSelf>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<TSelf>();
        }
    }
}
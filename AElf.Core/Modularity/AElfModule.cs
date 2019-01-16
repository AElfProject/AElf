using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Modularity
{
    public abstract class AElfModule : AbpModule
    {
        protected void ConfigureSelf<TOptions>() where TOptions : class
        {
            var configuration = ServiceConfigurationContext.Services.GetConfiguration();
            var optionKey = typeof(TOptions).Name.TrimEnd("Options");
            ServiceConfigurationContext.Services.Configure<TOptions>(configuration.GetSection(optionKey));
        }
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
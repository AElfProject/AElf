
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Net.Rpc
{
    public class NetRpcAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<NetRpcAElfModule>();

            /*
            builder.RegisterType<NetRpcService>().PropertiesAutowired();*/

        }

    }
}
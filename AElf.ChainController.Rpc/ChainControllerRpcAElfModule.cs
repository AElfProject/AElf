using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.ChainController.Rpc
{
    public class ChainControllerRpcAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<ChainControllerRpcAElfModule>();
            /*
            builder.RegisterType<TransactionResultService>().As<ITransactionResultService>().SingleInstance();
            builder.RegisterType<ChainControllerRpcService>().PropertiesAutowired();*/
        }

    }
}
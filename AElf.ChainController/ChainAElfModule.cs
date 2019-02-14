using AElf.ChainController.CrossChain;
using AElf.Kernel;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.ChainController
{
    [DependsOn(typeof(KernelAElfModule))]
    public class ChainControllerAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddAssemblyOf<ChainControllerAElfModule>();


            services.AddSingleton<ICrossChainInfoReader, CrossChainInfoReader>();
            /*builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
            builder.RegisterType<ChainCreationService>().As<IChainCreationService>();
            builder.RegisterType<ChainContextService>().As<IChainContextService>();
            builder.RegisterType<ChainService>().As<IChainService>().SingleInstance();
            builder.RegisterType<CrossChainInfoReader>().As<ICrossChainInfoReader>().SingleInstance();*/
        }
    }
}
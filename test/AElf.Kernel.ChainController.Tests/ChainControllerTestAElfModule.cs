using AElf.Kernel.ChainController.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Kernel.ChainController
{
    [DependsOn(
        typeof(ChainControllerAElfModule),
        typeof(KernelCoreTestAElfModule),
        typeof(TokenKernelAElfModule)
    )]
    public class ChainControllerTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddTransient<ChainCreationService>();
            context.Services.Replace(ServiceDescriptor
                .Singleton<ITransactionExecutingService, PlainTransactionExecutingService>());
        }

    }
}
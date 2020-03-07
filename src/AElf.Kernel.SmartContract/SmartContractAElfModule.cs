using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract
{
    [DependsOn(typeof(CoreKernelAElfModule))]
    public class SmartContractAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ISmartContractRunnerContainer, SmartContractRunnerContainer>();
            context.Services.AddSingleton<ITransactionExecutingService, PlainTransactionExecutingService>();
            context.Services.AddSingleton<IPlainTransactionExecutingService, PlainTransactionExecutingService>();
            context.Services.AddSingleton<IBestChainFoundLogEventProcessor, CodeCheckRequiredLogEventProcessor>();
        }

        public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
        {
            var deployedContractAddressService = context.ServiceProvider.GetService<IDeployedContractAddressService>();
            AsyncHelper.RunSync(() => deployedContractAddressService.InitAsync());
        }
    }
}
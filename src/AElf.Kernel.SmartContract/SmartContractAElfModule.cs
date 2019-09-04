using System.Runtime.CompilerServices;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

[assembly: InternalsVisibleTo("AElf.Kernel.SmartContract.Tests")]
namespace AElf.Kernel.SmartContract
{
    [DependsOn(typeof(CoreKernelAElfModule))]
    public class SmartContractAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ISmartContractRunnerContainer, SmartContractRunnerContainer>();
            
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var smartContractExecutiveService = context.ServiceProvider.GetService<ISmartContractExecutiveService>();
            AsyncHelper.RunSync(() => smartContractExecutiveService.InitContractInfoCacheAsync());
        }
    }
}
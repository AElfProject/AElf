using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Modularity;

namespace AElf.Runtime.CSharp
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class CSharpRuntimeAElfModule3 : AElfModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<RunnerOptions>(configuration.GetSection("Runner"));
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ISmartContractRunner, SmartContractRunnerForCategoryThree>(provider =>
            {
                var option = provider.GetService<IOptions<RunnerOptions>>();
                return new SmartContractRunnerForCategoryThree(
                    option.Value.SdkDir, provider.GetService<IServiceContainer<IExecutivePlugin>>(),
                    option.Value.BlackList, option.Value.WhiteList);
            });
        }

        public override void PostConfigureServices(ServiceConfigurationContext context)
        {
            //context.Services.RemoveAll(sd => sd.ImplementationType == typeof(CachedStateManager));
        }
    }
}
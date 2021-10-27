using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Modularity;

namespace AElf.Runtime.CSharp
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class CSharpRuntimeAElfModule : AElfModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<RunnerOptions>(configuration.GetSection("Runner"));
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ISmartContractRunner, CSharpSmartContractRunner>(provider =>
            {
                var option = provider.GetService<IOptions<RunnerOptions>>();
                return new CSharpSmartContractRunner(
                    option.Value.SdkDir);
            });
#if DEBUG
            context.Services.AddSingleton<ISmartContractRunner, UnitTestCSharpSmartContractRunner>(provider =>
            {
                var option = provider.GetService<IOptions<RunnerOptions>>();
                return new UnitTestCSharpSmartContractRunner(
                    option.Value.SdkDir);
            });
#endif
        }

        public override void PostConfigureServices(ServiceConfigurationContext context)
        {
            //context.Services.RemoveAll(sd => sd.ImplementationType == typeof(CachedStateManager));
        }
    }
}
using System.IO;
using AElf.Modularity;
using AElf.SmartContract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Modularity;

namespace AElf.Runtime.CSharp
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class CSharpRuntimeAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<RunnerOptions>(configuration.GetSection("Runner"));

            context.Services.AddSingleton<ISmartContractRunnerContainer>(provider =>
            {
                var option = provider.GetService<IOptions<RunnerOptions>>();
                var smartContractRunnerFactory = new SmartContractRunnerContainer();
                var runner =
                    new SmartContractRunner(option.Value.SdkDir, option.Value.BlackList, option.Value.WhiteList);
                smartContractRunnerFactory.AddRunner(0, runner);
                smartContractRunnerFactory.AddRunner(1, runner);

                return smartContractRunnerFactory;
            });
        }
    }
}
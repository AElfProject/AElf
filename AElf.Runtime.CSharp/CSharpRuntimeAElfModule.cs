using System.IO;
using AElf.Configuration.Config.Contract;
using AElf.Modularity;
using AElf.SmartContract;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Runtime.CSharp
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class CSharpRuntimeAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            RunnerConfig.Instance.SdkDir = Path.GetDirectoryName(typeof(CSharpRuntimeAElfModule).Assembly.Location);
            
            var runner = new SmartContractRunner();
            var smartContractRunnerFactory = new SmartContractRunnerContainer();
            smartContractRunnerFactory.AddRunner(0, runner);
            smartContractRunnerFactory.AddRunner(1, runner);
            context.Services.AddSingleton<ISmartContractRunnerContainer>(smartContractRunnerFactory);
        }
    }
}
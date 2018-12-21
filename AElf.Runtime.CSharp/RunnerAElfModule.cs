using System.IO;
using AElf.Configuration.Config.Contract;
using AElf.Modularity;
using AElf.SmartContract;
using Autofac;
using Volo.Abp.Modularity;

namespace AElf.Runtime.CSharp
{
    public class RunnerAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            RunnerConfig.Instance.SdkDir = Path.GetDirectoryName(typeof(RunnerAElfModule).Assembly.Location);
            
            var runner = new SmartContractRunner();
            var smartContractRunnerFactory = new SmartContractRunnerFactory();
            smartContractRunnerFactory.AddRunner(0, runner);
            smartContractRunnerFactory.AddRunner(1, runner);
            
            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerFactory>().SingleInstance();
        }

    }
}
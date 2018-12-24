using System.IO;
using AElf.Common.Module;
using AElf.Configuration.Config.Contract;
using AElf.SmartContract;
using Autofac;

namespace AElf.Runtime.CSharp
{
    public class RunnerAElfModule:IAElfModule
    {
        public void Init(ContainerBuilder builder)
        {
            RunnerConfig.Instance.SdkDir = Path.GetDirectoryName(typeof(RunnerAElfModule).Assembly.Location);
            
            var runner = new SmartContractRunner();
            var smartContractRunnerFactory = new SmartContractRunnerContainer();
            smartContractRunnerFactory.AddRunner(0, runner);
            smartContractRunnerFactory.AddRunner(1, runner);
            
            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerContainer>().SingleInstance();

        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}
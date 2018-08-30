using AElf.Common.Module;
using AElf.SmartContract;
using Autofac;

namespace AElf.Runtime.CSharp
{
    public class RunnerAElfModule:IAElfModlule
    {
        public void Init(ContainerBuilder builder)
        {
            var runner = new SmartContractRunner();
            var smartContractRunnerFactory = new SmartContractRunnerFactory();
            smartContractRunnerFactory.AddRunner(0, runner);
            smartContractRunnerFactory.AddRunner(1, runner);
            
            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerFactory>().SingleInstance();

        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}
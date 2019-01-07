using System.IO;
using AElf.Common.Module;
using AElf.Configuration.Config.Contract;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using Autofac;

namespace AElf.Launcher
{
    public class RunnerAElfModule : IAElfModule
    {
        public void Init(ContainerBuilder builder)
        {
            RunnerConfig.Instance.SdkDir = Path.GetDirectoryName(typeof(RunnerAElfModule).Assembly.Location);

            builder.RegisterType<SmartContractRunner>().As<ISmartContractRunner>();
            builder.RegisterType<SmartContractRunnerForCategoryOne>().As<ISmartContractRunner>();
            builder.RegisterType<SmartContractRunnerForCategoryTwo>().As<ISmartContractRunner>();
            builder.RegisterType<InjectedSmartContractRunnerContainer>().As<ISmartContractRunnerContainer>()
                .SingleInstance();
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}
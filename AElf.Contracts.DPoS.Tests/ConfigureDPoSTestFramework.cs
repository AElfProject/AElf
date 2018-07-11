using AElf.Database.Config;
using AElf.Kernel.KernelAccount;
using AElf.SmartContract;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Runtime.CSharp;
using Autofac;
using Xunit;
using Xunit.Abstractions;
using Xunit.Frameworks.Autofac;

[assembly: TestFramework("AElf.Contracts.DPoS.Tests.ConfigureDPoSTestFramework", "AElf.Contracts.DPoS.Tests")]

namespace AElf.Contracts.DPoS.Tests
{
    public class ConfigureDPoSTestFramework : AutofacTestFramework
    {
        public ConfigureDPoSTestFramework(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
        {
        }

        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new MainModule());
            builder.RegisterModule(new DatabaseModule());
            builder.RegisterModule(new LoggerModule());
            builder.RegisterModule(new StorageModule());
            builder.RegisterModule(new ServicesModule());
            builder.RegisterModule(new ManagersModule());
            builder.RegisterModule(new WorldStateDictatorModule());
            
            var smartContractRunnerFactory = new SmartContractRunnerFactory();
            var runner = new SmartContractRunner("../../../../AElf.Contracts.DPoS/bin/Debug/netstandard2.0/");
            smartContractRunnerFactory.AddRunner(0, runner);
            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerFactory>().SingleInstance();

            // configure your container
            // e.g. builder.RegisterModule<TestOverrideModule>();
        }
    }
}
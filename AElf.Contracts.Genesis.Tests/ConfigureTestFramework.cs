using AElf.Database.Config;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Runtime.CSharp;
using Autofac;
using Xunit;
using Xunit.Abstractions;
using Xunit.Frameworks.Autofac;

[assembly: TestFramework("AElf.Contracts.Genesis.Tests.ConfigureTestFramework", "AElf.Contracts.Genesis.Tests")]

namespace AElf.Contracts.Genesis.Tests
{
    public class ConfigureTestFramework : AutofacTestFramework
    {
        public ConfigureTestFramework(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
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
            var runner = new SmartContractRunner("../../../../AElf.Runtime.CSharp.Tests.TestContract/bin/Debug/netstandard2.0/");
            smartContractRunnerFactory.AddRunner(0, runner);
            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerFactory>().SingleInstance();
            // configure your container
            // e.g. builder.RegisterModule<TestOverrideModule>();
        }
    }
}
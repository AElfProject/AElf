using AElf.Database.Config;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Runtime.CSharp;
using Autofac;
using Xunit;
using Xunit.Abstractions;
using Xunit.Frameworks.Autofac;

[assembly: TestFramework("AElf.Kernel.Tests.ConfigureTestFramework", "AElf.Kernel.Tests")]

namespace AElf.Kernel.Tests
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
            builder.RegisterModule(new LoggerModule());
            builder.RegisterModule(new DatabaseModule(new DatabaseConfig()));
            builder.RegisterModule(new MetadataModule(Hash.Generate()));
            builder.RegisterModule(new WorldStateDictatorModule());

            var smartContractRunnerFactory = new SmartContractRunnerFactory();
            var runner = new SmartContractRunner(ContractCodes.TestContractFolder);
            smartContractRunnerFactory.AddRunner(0, runner);
            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerFactory>().SingleInstance();
            // configure your container
            // e.g. builder.RegisterModule<TestOverrideModule>();
        }
    }
}
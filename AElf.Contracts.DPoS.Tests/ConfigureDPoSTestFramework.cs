using AElf.Database.Config;
using AElf.Kernel.Modules.AutofacModule;
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
            builder.RegisterModule(new DatabaseModule(new DatabaseConfig()));

            // configure your container
            // e.g. builder.RegisterModule<TestOverrideModule>();
        }
    }
}
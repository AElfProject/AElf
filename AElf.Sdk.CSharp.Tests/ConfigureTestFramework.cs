using AElf.Database.Config;
using AElf.Kernel.Modules.AutofacModule;
using Autofac;
using Xunit;
using Xunit.Abstractions;
using Xunit.Frameworks.Autofac;

[assembly: TestFramework("AElf.Sdk.CSharp.Tests.ConfigureTestFramework", "AElf.Sdk.CSharp.Tests")]

namespace AElf.Sdk.CSharp.Tests
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
            builder.RegisterModule(new DatabaseModule(new DatabaseConfig()));

            // configure your container
            // e.g. builder.RegisterModule<TestOverrideModule>();
        }
    }
}
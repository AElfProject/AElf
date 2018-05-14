using AElf.Kernel.Modules.AutofacModule;
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
            builder.RegisterModule(new DatabaseModule());

            // configure your container
            // e.g. builder.RegisterModule<TestOverrideModule>();
        }
    }
}
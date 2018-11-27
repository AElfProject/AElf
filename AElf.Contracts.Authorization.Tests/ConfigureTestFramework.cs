using AElf.Common;
using Autofac;
using Xunit;
using Xunit.Abstractions;
using Xunit.Frameworks.Autofac;

[assembly: TestFramework("AElf.Contracts.Authorization.Tests.ConfigureTestFramework", "AElf.Contracts.Authorization.Tests")]

namespace AElf.Contracts.Authorization.Tests
{
    public class ConfigureTestFramework : AutofacTestFramework
    {
        public ConfigureTestFramework(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
        {
        }

        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new LoggerAutofacModule());
        }
    }
}
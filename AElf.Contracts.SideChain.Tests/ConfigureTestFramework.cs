using AElf.ChainController;
using AElf.Common;
using AElf.Database;
using AElf.Kernel;
using AElf.SmartContract;
using AElf.Runtime.CSharp;
using Autofac;
using Xunit;
using Xunit.Abstractions;
using Xunit.Frameworks.Autofac;

[assembly: TestFramework("AElf.Contracts.SideChain.Tests.ConfigureTestFramework", "AElf.Contracts.SideChain.Tests")]

namespace AElf.Contracts.SideChain.Tests
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
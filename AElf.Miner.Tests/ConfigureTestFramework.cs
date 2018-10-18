using AElf.ChainController;
using AElf.ChainController.TxMemPool;
using AElf.Common;
using AElf.Database;
using AElf.Execution;
using AElf.Execution.Scheduling;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using Autofac;
using Autofac.Core;
using Xunit;
using Xunit.Abstractions;
using Xunit.Frameworks.Autofac;

[assembly: TestFramework("AElf.Miner.Tests.ConfigureTestFramework", "AElf.Miner.Tests")]

namespace AElf.Miner.Tests
{
    public class ConfigureTestFramework : AutofacTestFramework
    {
        public ConfigureTestFramework(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new LoggerAutofacModule());
            builder.RegisterModule(new DatabaseAutofacModule());
            builder.RegisterType<DataStore>().As<IDataStore>();
        }
    }
}
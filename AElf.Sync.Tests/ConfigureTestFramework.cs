using AElf.ChainController;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using Autofac;
using NLog;
using Xunit;
using Xunit.Abstractions;
using Xunit.Frameworks.Autofac;

[assembly: TestFramework("AElf.Sync.Tests.ConfigureTestFramework", "AElf.Sync.Tests")]

namespace AElf.Sync.Tests
{
    public class ConfigureTestFramework : AutofacTestFramework
    {
        public ConfigureTestFramework(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterType<ChainService>().As<IChainService>();
            
            builder.RegisterType<ChainManagerBasic>().As<IChainManagerBasic>();
            builder.RegisterType<BlockManagerBasic>().As<IBlockManagerBasic>();
            builder.RegisterType<TransactionManager>().As<ITransactionManager>();
            builder.RegisterType<DataStore>().As<IDataStore>().SingleInstance();
            
            builder.RegisterType<Logger>().As<ILogger>();

            // configure your container
            // e.g. builder.RegisterModule<TestOverrideModule>();
        }
    }
}

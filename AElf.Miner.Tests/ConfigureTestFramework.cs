using AElf.ChainController;
using AElf.ChainController.TxMemPool;
using AElf.Execution;
using AElf.Execution.Scheduling;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using Autofac;
using Xunit;
using Xunit.Abstractions;
using Xunit.Frameworks.Autofac;

[assembly: TestFramework("AElf.Miner.Tests.ConfigureTestFramework", "AElf.MIner.Tests")]

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
            builder.RegisterModule(new MainModule());
            builder.RegisterModule(new LoggerModule());
            builder.RegisterModule(new DatabaseModule());
            builder.RegisterModule(new MetadataModule());
            builder.RegisterModule(new StateDictatorModule());
            builder.RegisterModule(new StorageModule());
            builder.RegisterModule(new ServicesModule());
            builder.RegisterModule(new ManagersModule());
            builder.RegisterInstance(new TxPoolConfig()).As<ITxPoolConfig>();
            builder.RegisterType<ContractTxPool>().As<IContractTxPool>().SingleInstance();
            builder.RegisterType<TxPoolService>().As<ITxPoolService>().SingleInstance();
            builder.RegisterType<ChainService>().As<IChainService>();
            builder.RegisterType<Grouper>().As<IGrouper>();
            builder.RegisterType<ServicePack>().PropertiesAutowired();
            builder.RegisterType<ActorEnvironment>().As<IActorEnvironment>().SingleInstance();
            builder.RegisterType<SimpleExecutingService>().As<IExecutingService>();

            var smartContractRunnerFactory = new SmartContractRunnerFactory();
            var runner = new SmartContractRunner(ContractCodes.TestContractFolder);
            smartContractRunnerFactory.AddRunner(0, runner);
            smartContractRunnerFactory.AddRunner(1, runner);
            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerFactory>().SingleInstance();
        }
    }
}
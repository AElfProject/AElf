using AElf.ChainController;
using AElf.ChainController.TxMemPool;
using AElf.Database;
using AElf.Execution;
using AElf.Execution.Scheduling;
using AElf.Kernel;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using Autofac;
using Autofac.Core;
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
            var assembly1 = typeof(IStateDictator).Assembly;
            builder.RegisterInstance<IHash>(new Hash()).As<Hash>();
            builder.RegisterAssemblyTypes(assembly1).AsImplementedInterfaces();
            var assembly2 = typeof(ISerializer<>).Assembly;
            builder.RegisterAssemblyTypes(assembly2).AsImplementedInterfaces();
            var assembly3 = typeof(StateDictator).Assembly;
            builder.RegisterAssemblyTypes(assembly3).AsImplementedInterfaces();
            var assembly4 = typeof(BlockVaildationService).Assembly;
            builder.RegisterAssemblyTypes(assembly4).AsImplementedInterfaces();
            var assembly5 = typeof(Execution.ParallelTransactionExecutingService).Assembly;
            builder.RegisterAssemblyTypes(assembly5).AsImplementedInterfaces();
            var assembly6 = typeof(AElf.Node.Node).Assembly;
            builder.RegisterAssemblyTypes(assembly6).AsImplementedInterfaces();
            var assembly7 = typeof(BlockHeader).Assembly;
            builder.RegisterAssemblyTypes(assembly7).AsImplementedInterfaces();
            builder.RegisterType(typeof(Hash)).As(typeof(IHash));
            builder.RegisterGeneric(typeof(Serializer<>)).As(typeof(ISerializer<>));
            
            builder.RegisterModule(new LoggerModule());
            builder.RegisterModule(new DatabaseAutofacModule());
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
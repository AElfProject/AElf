//using AElf.ChainController;
//using AElf.Common;
//using AElf.Database;
//using AElf.Kernel;
//using AElf.Kernel.Tests;
//using AElf.SmartContract;
//using Autofac;
//using Xunit;
//using Xunit.Abstractions;
//using Xunit.Frameworks.Autofac;
//using AElf.Runtime.CSharp;
//
//[assembly: TestFramework("AElf.Runtime.CSharp2.Tests.ConfigureTestFramework", "AElf.Runtime.CSharp2.Tests")]
//
//namespace AElf.Runtime.CSharp2.Tests
//{
//    public class ConfigureTestFramework : AutofacTestFramework
//    {
//        public ConfigureTestFramework(IMessageSink diagnosticMessageSink)
//            : base(diagnosticMessageSink)
//        {
//        }
//
//        protected override void ConfigureContainer(ContainerBuilder builder)
//        {
//            var assembly1 = typeof(IDataProvider).Assembly;
//            builder.RegisterAssemblyTypes(assembly1).AsImplementedInterfaces();
//            var assembly3 = typeof(DataProvider).Assembly;
//            builder.RegisterAssemblyTypes(assembly3).AsImplementedInterfaces();
//            var assembly4 = typeof(BlockValidationService).Assembly;
//            builder.RegisterAssemblyTypes(assembly4).AsImplementedInterfaces();
//            var assembly5 = typeof(Execution.ParallelTransactionExecutingService).Assembly;
//            builder.RegisterAssemblyTypes(assembly5).AsImplementedInterfaces();
//            var assembly6 = typeof(AElf.Node.Node).Assembly;
//            builder.RegisterAssemblyTypes(assembly6).AsImplementedInterfaces();
//            var assembly7 = typeof(BlockHeader).Assembly;
//            builder.RegisterAssemblyTypes(assembly7).AsImplementedInterfaces();
//            
//            builder.RegisterModule(new DatabaseAutofacModule());
//            builder.RegisterModule(new LoggerAutofacModule());
//            builder.RegisterModule(new ChainAutofacModule());
//            builder.RegisterModule(new KernelAutofacModule());
//            builder.RegisterModule(new SmartContractAutofacModule());
//            
//            var smartContractRunnerFactory = new SmartContractRunnerContainer();
//            var runner0 = new SmartContractRunner(ContractCodes.TestContractZeroFolder);
//            var runner = new SmartContractRunnerForCategoryTwo(
//                "../../../../AElf.Sdk.CSharp2.Tests.TestContract/bin/Debug/netstandard2.0/"
//            );
//            smartContractRunnerFactory.AddRunner(0, runner0);
//            smartContractRunnerFactory.AddRunner(2, runner);
//            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerContainer>().SingleInstance();
//            // configure your container
//            // e.g. builder.RegisterModule<TestOverrideModule>();
//        }
//    }
//}
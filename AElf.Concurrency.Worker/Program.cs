using System;
using AElf.Database;
using AElf.Execution;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using Autofac;
using NLog;

namespace AElf.Concurrency.Worker
{
    class Program
    {
        private static ILogger _logger = LogManager.GetCurrentClassLogger();
        
        static void Main(string[] args)
        {
            var confParser = new ConfigParser();
            bool parsed;
            try
            {
                parsed = confParser.Parse(args);
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw;
            }

            if (!parsed)
                return;

            var runner = new SmartContractRunner();
            var smartContractRunnerFactory = new SmartContractRunnerFactory();
            smartContractRunnerFactory.AddRunner(0, runner);
            smartContractRunnerFactory.AddRunner(1, runner);

            // Setup ioc 
            var container = SetupIocContainer(true, smartContractRunnerFactory);
            if (container == null)
            {
                _logger.Error("IoC setup failed");
                return;
            }

            if (!CheckDBConnect(container))
            {
                _logger.Error("Database connection failed");
                return;
            }

            using (var scope = container.BeginLifetimeScope())
            {
                var service = scope.Resolve<ActorEnvironment>();
                service.InitWorkActorSystem();
                Console.WriteLine("Press Control + C to terminate.");
                Console.CancelKeyPress += async (sender, eventArgs) => { await service.StopAsync(); };
                service.TerminationHandle.Wait();
            }
        }

        private static IContainer SetupIocContainer(bool isMiner, SmartContractRunnerFactory smartContractRunnerFactory)
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule(new MainModule()); // todo : eventually we won't need this

            // Module registrations
            builder.RegisterModule(new TransactionManagerModule());
            builder.RegisterModule(new LoggerModule());
            builder.RegisterModule(new DatabaseModule());
            builder.RegisterModule(new NetworkModule(isMiner));
            builder.RegisterModule(new MinerModule(null));
            builder.RegisterModule(new StateDictatorModule());
            builder.RegisterModule(new StorageModule());
            builder.RegisterModule(new ServicesModule());
            builder.RegisterModule(new ManagersModule());
            builder.RegisterModule(new MetadataModule());

            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerFactory>().SingleInstance();
            builder.RegisterType<ServicePack>().PropertiesAutowired();
            builder.RegisterType<ActorEnvironment>().SingleInstance();
            IContainer container;
            try
            {
                container = builder.Build();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return null;
            }
            return container;
        }

        private static bool CheckDBConnect(IComponentContext container)
        {
            var db = container.Resolve<IKeyValueDatabase>();
            try
            {
                return db.IsConnected();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return false;
            }
        }
    }
}
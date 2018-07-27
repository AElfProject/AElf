using System;
using AElf.Database;
using AElf.Execution;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Launcher;
using AElf.Network.Config;
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

            var netConf = confParser.NetConfig;
            var isMiner = confParser.IsMiner;

            var runner = new SmartContractRunner(confParser.RunnerConfig);
            var smartContractRunnerFactory = new SmartContractRunnerFactory();
            smartContractRunnerFactory.AddRunner(0, runner);
            smartContractRunnerFactory.AddRunner(1, runner);

            // Setup ioc 
            var container = SetupIocContainer(isMiner, netConf, smartContractRunnerFactory);
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
                var service = scope.Resolve<IConcurrencyExecutingService>();
                service.InitWorkActorSystem();
                Console.WriteLine("Press Control + C to terminate.");
                Console.CancelKeyPress += async (sender, eventArgs) => { await service.StopAsync(); };
                service.TerminationHandle.Wait();
            }
        }

        private static IContainer SetupIocContainer(bool isMiner, IAElfNetworkConfig netConf,
            SmartContractRunnerFactory smartContractRunnerFactory)
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule(new MainModule()); // todo : eventually we won't need this

            // Module registrations
            builder.RegisterModule(new TransactionManagerModule());
            builder.RegisterModule(new LoggerModule());
            builder.RegisterModule(new DatabaseModule());
            builder.RegisterModule(new NetworkModule(netConf, isMiner));
            builder.RegisterModule(new RpcServerModule());
            builder.RegisterModule(new MinerModule(null));
            builder.RegisterModule(new WorldStateDictatorModule());
            builder.RegisterModule(new StorageModule());
            builder.RegisterModule(new ServicesModule());
            builder.RegisterModule(new ManagersModule());
            builder.RegisterModule(new MetadataModule());

            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerFactory>().SingleInstance();

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
            return db.IsConnected();
        }
    }
}
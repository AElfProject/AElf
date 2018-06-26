using System;
using System.IO;
using System.Net;
using System.Security;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Database;
using AElf.Database.Config;
using AElf.Kernel;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Execution.Config;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Miner;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Kernel.Node;
using AElf.Kernel.Node.Config;
using AElf.Kernel.TxMemPool;
using AElf.Launcher;
using AElf.Network.Config;
using AElf.Runtime.CSharp;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Configuration;
using Autofac;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.Concurrency.Worker
{
    class Program
    {

        static void Main(string[] args)
        {
            var typename = typeof(Akka.Remote.Serialization.ProtobufSerializer).FullName;
            var type = Type.GetType("AElf.Kernel.Concurrency.Execution.Messages.JobExecutionRequest, AElf.Kernel");
            // Parse options
            ConfigParser confParser = new ConfigParser();
            bool parsed;
            try
            {
                parsed = confParser.Parse(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            if (!parsed)
                return;
            
            var txPoolConf = confParser.TxPoolConfig;
            var netConf = confParser.NetConfig;
            var databaseConf = confParser.DatabaseConfig;
            var minerConfig = confParser.MinerConfig;
            var nodeConfig = confParser.NodeConfig;
            var isMiner = confParser.IsMiner;
            var isNewChain = confParser.NewChain;
            var initData = confParser.InitData;
            
            // Setup ioc 
            IContainer container = SetupIocContainer(isMiner, isNewChain, netConf, databaseConf, txPoolConf, 
                minerConfig, nodeConfig);

            if (container == null)
            {
                Console.WriteLine("IoC setup failed");
                return;
            }

            if (!CheckDBConnect(container))
            {
                Console.WriteLine("Database connection failed");
                return;
            }

            // todo : quick fix, to be refactored

            using(var scope = container.BeginLifetimeScope())
            {
                var port = args.Length == 0 ? 32551 : Convert.ToInt32(args[0]);
                var service = new ConcurrencyExecutingService();
                service.InitWorkActorSystem("127.0.0.1", port);

                Console.ReadLine();
            }
            
        }
        
        private static void InitWorkActorSystem(string ip,int port)
        {

            var config = ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.port=" + port)
                //.WithFallback(ConfigurationFactory.ParseString("akka.cluster.seed-nodes = [\"akka.tcp://ClusterSystem@" + args[0] + ":32551\"]"))
                .WithFallback(ActorWorkerConfig.Instance.HoconContent);
            
            var actorSystem = ActorSystem.Create("AElfSystem",config);
            actorSystem.ActorOf(Props.Create<Kernel.Concurrency.Execution.Worker>(), "worker");
        }

        private static IContainer SetupIocContainer(bool isMiner, bool isNewChain, IAElfNetworkConfig netConf,
            IDatabaseConfig databaseConf, ITxPoolConfig txPoolConf, IMinerConfig minerConf, INodeConfig nodeConfig)
        {
            var builder = new ContainerBuilder();
            
            builder.RegisterModule(new MainModule()); // todo : eventually we won't need this
            
            // Module registrations
            builder.RegisterModule(new TransactionManagerModule());
            builder.RegisterModule(new LoggerModule());
            builder.RegisterModule(new DatabaseModule(databaseConf));
            builder.RegisterModule(new NetworkModule(netConf, isMiner));
            builder.RegisterModule(new RpcServerModule());
            builder.RegisterModule(new MinerModule(null));
            builder.RegisterModule(new MetadataModule());
            
            var runner = new SmartContractRunner("../AElf.SDK.CSharp/bin/Debug/netstandard2.0/");
            SmartContractRunnerFactory smartContractRunnerFactory = new SmartContractRunnerFactory();
            smartContractRunnerFactory.AddRunner(0, runner);
            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerFactory>().SingleInstance();
            
            IContainer container = null;
            
            try
            {
                container = builder.Build();
            }
            catch (Exception e)
            {
                return null;
            }

            return container;
        }

        private static bool CheckDBConnect(IContainer container)
        {
            var db = container.Resolve<IKeyValueDatabase>();
            return db.IsConnected();
        }
        
    }
}
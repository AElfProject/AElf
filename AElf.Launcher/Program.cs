using System;
using System.IO;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common.ByteArrayHelpers;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Database;
using AElf.Execution;
using AElf.Kernel;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Kernel.Node;
using AElf.Configuration;
using AElf.Configuration.Config.Network;
using AElf.Execution.Scheduling;
using AElf.Network;
using AElf.Runtime.CSharp;
using AElf.SideChain.Creation;
using AElf.SmartContract;
using Autofac;
using Easy.MessageHub;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceStack;
using IContainer = Autofac.IContainer;
using RpcServer = AElf.RPC.RpcServer;

namespace AElf.Launcher
{
    class Program
    {
        private static string AssemblyDir { get; } = Path.GetDirectoryName(typeof(Program).Assembly.Location);
        private const string FilePath = @"ChainInfo.json";
        private static int _stopped;

        static void Main(string[] args)
        {
            // Parse options
            var confParser = new ConfigParser();
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

            var minerConfig = confParser.MinerConfig;
            var isMiner = confParser.IsMiner;
            var isNewChain = confParser.NewChain;
            var initData = confParser.InitData;
            NodeConfig.Instance.IsChainCreator = confParser.NewChain;
            NodeConfig.Instance.ConsensusInfoGenerater = confParser.IsConsensusInfoGenerater;

            var runner = new SmartContractRunner(confParser.RunnerConfig);
            var smartContractRunnerFactory = new SmartContractRunnerFactory();
            smartContractRunnerFactory.AddRunner(0, runner);
            smartContractRunnerFactory.AddRunner(1, runner);

            // todo : quick fix, to be refactored
            ECKeyPair nodeKey = null;
            if (!string.IsNullOrWhiteSpace(confParser.NodeAccount))
            {
                try
                {
                    var ks = new AElfKeyStore(NodeConfig.Instance.DataDir);
                    var pass = string.IsNullOrWhiteSpace(confParser.NodeAccountPassword)
                        ? AskInvisible(confParser.NodeAccount)
                        : confParser.NodeAccountPassword;
                    ks.OpenAsync(confParser.NodeAccount, pass, false);

                    ManagementConfig.Instance.NodeAccount = confParser.NodeAccount;
                    ManagementConfig.Instance.NodeAccountPassword = pass;
                    
                    nodeKey = ks.GetAccountKeyPair(confParser.NodeAccount);
                    if (nodeKey == null)
                    {
                        Console.WriteLine("Load keystore failed");
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Load keystore failed");
                }
            }

            var txPoolConf = confParser.TxPoolConfig;
            txPoolConf.EcKeyPair = nodeKey;

            // Setup ioc 
            var container = SetupIocContainer(isMiner, isNewChain, txPoolConf,
                minerConfig, smartContractRunnerFactory);

            if (container == null)
            {
                Console.WriteLine("IoC setup failed");
                return;
            }

            if (!CheckDbConnect(container))
            {
                Console.WriteLine("Database connection failed");
                return;
            }


            using (var scope = container.BeginLifetimeScope())
            {
                IActorEnvironment actorEnv = null;
                if (NodeConfig.Instance.ExecutorType == "akka")
                {
                    actorEnv = scope.Resolve<IActorEnvironment>();
                    actorEnv.InitActorSystem();   
                }

                var evListener = scope.Resolve<ChainCreationEventListener>();
                MessageHub.Instance.Subscribe<IBlock>(async (t) =>
                {
                    await evListener.OnBlockAppended(t);
                });
                
                var node = scope.Resolve<IAElfNode>();
                // Start the system
                node.Start(nodeKey, TokenGenesisContractCode, ConsensusGenesisContractCode, BasicContractZero);

                var txPoolService = scope.Resolve<ITxPoolService>();
                MessageHub.Instance.Subscribe<IncomingTransaction>(
                    async (inTx) => { await txPoolService.AddTxAsync(inTx.Transaction); });

                var netManager = scope.Resolve<INetworkManager>();
                MessageHub.Instance.Subscribe<TransactionAddedToPool>(
                    async (txAdded) =>
                    {
                        await netManager.BroadcastMessage(AElfProtocolType.BroadcastTx,
                            txAdded.Transaction.Serialize());
                    }
                );

                if (confParser.Rpc)
                {
                    var rpc = new RpcServer();
                    rpc.Initialize(scope, confParser.RpcHost, confParser.RpcPort);
                    rpc.RunAsync();
                }

                //DoDPos(node);
                if (actorEnv!=null)
                {
                    Console.CancelKeyPress += async (sender, eventArgs) => { await actorEnv.StopAsync(); };
                    actorEnv.TerminationHandle.Wait();
                }

                Console.CancelKeyPress += (s, e) => { Interlocked.CompareExchange(ref _stopped, 1, 0); };
                while (_stopped == 0)
                {
                    Console.ReadKey();
                }
            }
        }

        private static byte[] TokenGenesisContractCode
        {
            get
            {
                var contractZeroDllPath = Path.Combine(AssemblyDir, $"{Globals.GenesisTokenContractAssemblyName}.dll");

                byte[] code;
                using (var file = File.OpenRead(Path.GetFullPath(contractZeroDllPath)))
                {
                    code = file.ReadFully();
                }

                return code;
            }
        }

        private static byte[] ConsensusGenesisContractCode
        {
            get
            {
                var contractZeroDllPath =
                    Path.Combine(AssemblyDir, $"{Globals.GenesisConsensusContractAssemblyName}.dll");

                byte[] code;
                using (var file = File.OpenRead(Path.GetFullPath(contractZeroDllPath)))
                {
                    code = file.ReadFully();
                }

                return code;
            }
        }

        private static byte[] BasicContractZero
        {
            get
            {
                var contractZeroDllPath =
                    Path.Combine(AssemblyDir, $"{Globals.GenesisSmartContractZeroAssemblyName}.dll");

                byte[] code;
                using (var file = File.OpenRead(Path.GetFullPath(contractZeroDllPath)))
                {
                    code = file.ReadFully();
                }

                return code;
            }
        }

        private static IContainer SetupIocContainer(bool isMiner, bool isNewChain, 
            ITxPoolConfig txPoolConf, IMinerConfig minerConf,
            SmartContractRunnerFactory smartContractRunnerFactory)
        {
            var builder = new ContainerBuilder();

            // Register everything
            builder.RegisterModule(new MainModule()); // todo : eventually we won't need this

            // Module registrations
            builder.RegisterModule(new ManagersModule());
            builder.RegisterModule(new MetadataModule());
            builder.RegisterModule(new TransactionManagerModule());
            builder.RegisterModule(new WorldStateDictatorModule());
            builder.RegisterModule(new LoggerModule("aelf-node-" + NetworkConfig.Instance.ListeningPort));
            builder.RegisterModule(new DatabaseModule());
            builder.RegisterModule(new NetworkModule(isMiner));
            builder.RegisterModule(new RpcServicesModule());
            builder.RegisterType<ChainService>().As<IChainService>();
            builder.RegisterType<ChainCreationEventListener>().PropertiesAutowired();

            if (NodeConfig.Instance.ExecutorType == "akka")
            {
                builder.RegisterType<ResourceUsageDetectionService>().As<IResourceUsageDetectionService>();
                builder.RegisterType<Grouper>().As<IGrouper>();
                builder.RegisterType<ServicePack>().PropertiesAutowired();
                builder.RegisterType<ActorEnvironment>().As<IActorEnvironment>().SingleInstance();
                builder.RegisterType<ParallelTransactionExecutingService>().As<IExecutingService>();
            }
            else
            {
                builder.RegisterType<SimpleExecutingService>().As<IExecutingService>();
            }
            
            // register SmartContractRunnerFactory 
            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerFactory>().SingleInstance();

            Hash chainId;
            if (isNewChain)
            {
                chainId = Hash.Generate().ToChainId();
                var obj = new JObject(new JProperty("id", chainId.ToHex()));

                // write JSON directly to a file
                using (var file = File.CreateText(FilePath))
                using (var writer = new JsonTextWriter(file))
                {
                    obj.WriteTo(writer);
                }
            }
            else
            {
                // read JSON directly from a file
                using (var file = File.OpenText(FilePath))
                using (var reader = new JsonTextReader(file))
                {
                    var chain = (JObject) JToken.ReadFrom(reader);
                    chainId = ByteArrayHelpers.FromHexString(chain.GetValue("id").ToString());
                }
            }

            // register miner config
            var minerConfiguration = isMiner ? minerConf : MinerConfig.Default;
            minerConfiguration.ChainId = chainId;
            builder.RegisterModule(new MinerModule(minerConfiguration));

            NodeConfig.Instance.ChainId = chainId.Value.ToByteArray().ToHex();
            builder.RegisterModule(new MainChainNodeModule());

            txPoolConf.ChainId = chainId;
            builder.RegisterModule(new TxPoolServiceModule(txPoolConf));

            IContainer container;
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

        private static bool CheckDbConnect(IComponentContext container)
        {
            var db = container.Resolve<IKeyValueDatabase>();
            try
            {
                return db.IsConnected();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        private static string AskInvisible(string prefix)
        {
            Console.Write("Node account password: ");
            var pwd = new SecureString();
            while (true)
            {
                var i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }

                if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.RemoveAt(pwd.Length - 1);
                    }
                }
                else
                {
                    pwd.AppendChar(i.KeyChar);
                }
            }

            Console.WriteLine();
            return new NetworkCredential("", pwd).Password;
        }
    }
}
using System;
using System.IO;
using System.Net;
using System.Security;
using System.Threading;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.ChainController.TxMemPool;
using AElf.Common.ByteArrayHelpers;
using AElf.Common.Extensions;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Database;
using AElf.Execution;
using AElf.Kernel;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Kernel.Node;
using AElf.Configuration;
using AElf.Configuration.Config.Network;
using AElf.Miner.Miner;
using AElf.Execution.Scheduling;
using AElf.Network;
using AElf.Node;
using AElf.Node.AElfChain;
using AElf.RPC;
using AElf.Runtime.CSharp;
using AElf.SideChain.Creation;
using AElf.SmartContract;
using Autofac;
using Easy.MessageHub;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IContainer = Autofac.IContainer;

namespace AElf.Launcher
{
    class Program
    {
        private const string FilePath = @"ChainInfo.json";
        private static int _stopped;
        private static readonly AutoResetEvent _closing = new AutoResetEvent(false);

        static void Main(string[] args)
        {
            // Parse options
            Console.WriteLine(string.Join(" ",args));
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
            var chainId = confParser.ChainId;
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
            var container = SetupIocContainer(isMiner, isNewChain, chainId, txPoolConf, minerConfig, smartContractRunnerFactory);

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
                
                /************** Node setup ***************/
                
                NodeConfiguration confContext = new NodeConfiguration();
                confContext.KeyPair = nodeKey;
                confContext.WithRpc = confParser.Rpc;
                confContext.LauncherAssemblyLocation = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                
                var mainChainNodeService = scope.Resolve<INodeService>();
                var rpc = scope.Resolve<IRpcServer>();
                rpc.Init(scope, confParser.RpcHost, confParser.RpcPort);

                var node = scope.Resolve<INode>();
                node.Register(mainChainNodeService);
                node.Initialize(confContext);
                node.Start();
                
                /*****************************************/

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
                
                if (actorEnv != null)
                {
                    Console.CancelKeyPress += async (sender, eventArgs) => { await actorEnv.StopAsync(); };
                    actorEnv.TerminationHandle.Wait();
                }

                Console.CancelKeyPress += OnExit;
                _closing.WaitOne();
            }
        }
        
        protected static void OnExit(object sender, ConsoleCancelEventArgs args)
        {
            _closing.Set();
        }

        private static IContainer SetupIocContainer(bool isMiner, bool isNewChain, string chainId,
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
            builder.RegisterType<MainchainNodeService>().As<INodeService>();
            builder.RegisterType<RpcServer>().As<IRpcServer>().SingleInstance();
            
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

            Hash chainIdHash;
            if (isNewChain)
            {
                if (string.IsNullOrWhiteSpace(chainId))
                {
                    chainIdHash = Hash.Generate().ToChainId();
                }
                else
                {
                    chainIdHash = ByteArrayHelpers.FromHexString(chainId);
                }

                var obj = new JObject(new JProperty("id", chainIdHash.ToHex()));

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
                    chainIdHash = ByteArrayHelpers.FromHexString(chain.GetValue("id").ToString());
                }
            }

            // register miner config
            var minerConfiguration = isMiner ? minerConf : MinerConfig.Default;
            minerConfiguration.ChainId = chainIdHash;
            builder.RegisterModule(new MinerModule(minerConfiguration));

            NodeConfig.Instance.ChainId = chainIdHash.Value.ToByteArray().ToHex();
            builder.RegisterModule(new MainChainNodeModule());

            txPoolConf.ChainId = chainIdHash;
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
using System;
using System.IO;
using System.Net;
using System.Security;
using AElf.ChainController;
using AElf.Common.ByteArrayHelpers;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Database;
using AElf.Execution;
using AElf.Kernel;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Kernel.Node;
using AElf.Kernel.Node.Config;
using AElf.Network.Config;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using Autofac;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceStack;
using IContainer = Autofac.IContainer;

namespace AElf.Launcher
{
    class Program
    {
        private static string AssemblyDir { get; } = Path.GetDirectoryName(typeof(Program).Assembly.Location);
        private const string FilePath = @"ChainInfo.json";

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

            var netConf = confParser.NetConfig;
            var minerConfig = confParser.MinerConfig;
            var nodeConfig = confParser.NodeConfig;
            var isMiner = confParser.IsMiner;
            var isNewChain = confParser.NewChain;
            var initData = confParser.InitData;
            nodeConfig.IsChainCreator = confParser.NewChain;
            nodeConfig.ConsensusInfoGenerater = confParser.IsConsensusInfoGenerater;

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
                    var ks = new AElfKeyStore(nodeConfig.DataDir);
                    var pass = string.IsNullOrWhiteSpace(confParser.NodeAccountPassword) ? AskInvisible(confParser.NodeAccount) : confParser.NodeAccountPassword;
                    ks.OpenAsync(confParser.NodeAccount, pass, false);

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
            var container = SetupIocContainer(isMiner, isNewChain, netConf, txPoolConf,
                minerConfig, nodeConfig, smartContractRunnerFactory);

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
                var concurrencySercice = scope.Resolve<IConcurrencyExecutingService>();
                concurrencySercice.InitActorSystem();

                var node = scope.Resolve<IAElfNode>();

                // Start the system
                node.Start(nodeKey, confParser.Rpc, confParser.RpcPort, confParser.RpcHost, initData,
                    TokenGenesisContractCode, ConsensusGenesisContractCode, BasicContractZero);

                //DoDPos(node);
                Console.ReadLine();
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
                var contractZeroDllPath = Path.Combine(AssemblyDir, $"{Globals.GenesisConsensusContractAssemblyName}.dll");

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
                var contractZeroDllPath = Path.Combine(AssemblyDir, $"{Globals.GenesisSmartContractZeroAssemblyName}.dll");

                byte[] code;
                using (var file = File.OpenRead(Path.GetFullPath(contractZeroDllPath)))
                {
                    code = file.ReadFully();
                }

                return code;
            }
        }

        private static IContainer SetupIocContainer(bool isMiner, bool isNewChain, IAElfNetworkConfig netConf,
            ITxPoolConfig txPoolConf, IMinerConfig minerConf, INodeConfig nodeConfig,
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
            builder.RegisterModule(new LoggerModule("aelf-node-" + netConf.ListeningPort));
            builder.RegisterModule(new DatabaseModule());
            builder.RegisterModule(new NetworkModule(netConf, isMiner));
            builder.RegisterModule(new RpcServerModule());
            builder.RegisterType<ChainService>().As<IChainService>();

            // register SmartContractRunnerFactory 
            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerFactory>().SingleInstance();

            Hash chainId;
            if (isNewChain)
            {
                chainId = Hash.Generate();
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

            nodeConfig.ChainId = chainId;
            builder.RegisterModule(new MainChainNodeModule(nodeConfig));

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
            return db.IsConnected();
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
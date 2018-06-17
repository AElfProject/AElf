using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Database;
using AElf.Database.Config;
using AElf.Kernel;
using AElf.Kernel.Miner;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Kernel.Node;
using AElf.Kernel.Node.Config;
using AElf.Kernel.TxMemPool;
using AElf.Network.Config;
using Autofac;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IContainer = Autofac.IContainer;

namespace AElf.Launcher
{
    class Program
    {
        private const string filepath = @"ChainInfo.json";

        static void Main(string[] args)
        {
            // Parse options
            ConfigParser confParser = new ConfigParser();
            bool parsed = confParser.Parse(args);

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
            IContainer container = SetupIocContainer(isMiner, isNewChain, netConf, databaseConf, txPoolConf, minerConfig, nodeConfig);

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
            
            ECKeyPair nodeKey = null;
            if (!string.IsNullOrWhiteSpace(confParser.NodeAccount))
            {
                try
                {
                    AElfKeyStore ks = new AElfKeyStore(confParser.DataDir);

                    string pass = AskInvisible(confParser.NodeAccount);
                    ks.OpenAsync(confParser.NodeAccount, pass, false);

                    nodeKey = ks.GetAccountKeyPair(confParser.NodeAccount);
                }
                catch (Exception e)
                {
                }
            }

            using(var scope = container.BeginLifetimeScope())
            {
                IAElfNode node = scope.Resolve<IAElfNode>();
               
                // Start the system
                node.Start(nodeKey, confParser.Rpc, initData);

                Console.ReadLine();
            }
        }

        private static IContainer SetupIocContainer(bool isMiner, bool isNewChain, IAElfNetworkConfig netConf, 
            IDatabaseConfig databaseConf, ITxPoolConfig txPoolConf, IMinerConfig minerConf, INodeConfig nodeConfig)
        {
            var builder = new ContainerBuilder();
            
            // Register everything
            builder.RegisterModule(new MainModule()); // todo : eventually we won't need this
            
            // Module registrations
            builder.RegisterModule(new TransactionManagerModule());
            builder.RegisterModule(new LoggerModule());
            builder.RegisterModule(new DatabaseModule(databaseConf));
            builder.RegisterModule(new NetworkModule(netConf));
            builder.RegisterModule(new RpcServerModule());

            Hash chainId;
            if (isNewChain)
            {
                chainId = Hash.Generate();
                JObject obj = new JObject(new JProperty("id", chainId.ToByteString().ToBase64()));

                // write JSON directly to a file
                using (StreamWriter file = File.CreateText(filepath))
                using (JsonTextWriter writer = new JsonTextWriter(file))
                {
                    obj.WriteTo(writer);
                }
            }
            else
            {
                // read JSON directly from a file
                using (StreamReader file = File.OpenText(filepath))
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JObject chain = (JObject)JToken.ReadFrom(reader);
                    chainId = new Hash(Convert.FromBase64String(chain.GetValue("id").ToString()));
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
        
        public static string AskInvisible(string prefix)
        {
            Console.Write("Node account password: ");
            
            var pwd = new SecureString();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.RemoveAt(pwd.Length - 1);
                    }
                }
                else
                {
                    pwd.AppendChar(i.KeyChar);
                    //Console.Write("*");
                }
            }
            
            Console.WriteLine();
            
            return new NetworkCredential("", pwd).Password;
        }
    }
}
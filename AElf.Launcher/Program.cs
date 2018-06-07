using System;
using System.IO;
using AElf.Database;
using AElf.Database.Config;
using AElf.Kernel;
using AElf.Kernel.Miner;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Kernel.Node;
using AElf.Kernel.Node.Config;
using AElf.Kernel.Node.Network.Config;
using AElf.Kernel.TxMemPool;
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

            using(var scope = container.BeginLifetimeScope())
            {
                IAElfNode node = scope.Resolve<IAElfNode>();
               
                // Start the system
                node.Start(confParser.Rpc);

                Console.ReadLine();
            }
        }

        private static IContainer SetupIocContainer(bool isMiner, bool isNewChain, IAElfNetworkConfig netConf, IDatabaseConfig databaseConf, ITxPoolConfig txPoolConf, IMinerConfig minerConf, INodeConfig nodeConfig)
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
            builder.RegisterModule(new TxPoolServiceModule(txPoolConf));

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
                    chainId = new Hash(ByteString.CopyFromUtf8(chain.GetValue("id").ToString()));
                }
            }

            // register miner config
            var minerConfiguration = isMiner ? minerConf : MinerConfig.Default;
            minerConfiguration.ChainId = chainId;
            builder.RegisterModule(new MinerModule(minerConfiguration));

            nodeConfig.ChainId = chainId;
            builder.RegisterModule(new MainChainNodeModule(nodeConfig));

                          
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
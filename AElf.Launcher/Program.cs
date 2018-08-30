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


            // todo : quick fix, to be refactored
            ECKeyPair nodeKey = null;
            
            // Setup ioc 
            var container = SetupIocContainer(isMiner, minerConfig);


            using (var scope = container.BeginLifetimeScope())
            {
                
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
            }
        }

        private static IContainer SetupIocContainer(bool isMiner, IMinerConfig minerConf)
        {
            var builder = new ContainerBuilder();

            // Register everything
            builder.RegisterModule(new MainModule()); // todo : eventually we won't need this

            // Module registrations
            builder.RegisterModule(new ManagersModule());
            builder.RegisterModule(new MetadataModule());
            builder.RegisterModule(new TransactionManagerModule());
            builder.RegisterModule(new StateDictatorModule());
            builder.RegisterModule(new LoggerModule("aelf-node-" + NetworkConfig.Instance.ListeningPort));
            builder.RegisterModule(new NetworkModule(isMiner));
            builder.RegisterModule(new RpcServicesModule());
            builder.RegisterModule(new StorageModule());
            builder.RegisterType<ChainService>().As<IChainService>();
            builder.RegisterType<ChainCreationEventListener>().PropertiesAutowired();
            builder.RegisterType<MainchainNodeService>().As<INodeService>();
            builder.RegisterType<RpcServer>().As<IRpcServer>().SingleInstance();
            

            Hash chainIdHash=null;

            // register miner config
            var minerConfiguration = isMiner ? minerConf : MinerConfig.Default;
            minerConfiguration.ChainId = chainIdHash;
            builder.RegisterModule(new MinerModule(minerConfiguration));

            NodeConfig.Instance.ChainId = chainIdHash.Value.ToByteArray().ToHex();
            builder.RegisterModule(new MainChainNodeModule());

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
    }
}
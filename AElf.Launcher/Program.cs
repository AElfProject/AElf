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
using AElf.Common.Module;
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
using AElf.Miner;
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
            Console.WriteLine(string.Join(" ",args));

            var parsed = new CommandLineParser();
            parsed.Parse(args);

            var handler = new AElfModuleHandler();
            handler.Register(new DatabaseAElfModule());
            handler.Register(new KernelAElfModule());
            handler.Register(new SmartContractAElfModule());
            handler.Register(new ChainAElfModule());
            handler.Register(new ExecutionAElfModule());
            handler.Register(new NodeAElfModule());
            handler.Register(new MinerAElfModule());
            
            
            
            handler.Register(new KernelAElfModule());
            handler.Register(new KernelAElfModule());
            handler.Register(new KernelAElfModule());
            handler.Register(new KernelAElfModule());
            handler.Register(new KernelAElfModule());
            handler.Register(new KernelAElfModule());
            
            
            
            
            handler.Register(new LauncherAElfModule());
            handler.Build();
                
                
            
            // Parse options
            Console.WriteLine(string.Join(" ",args));
            var confParser = new ConfigParser();
            

            try
            {
                confParser.Parse(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            var minerConfig = confParser.MinerConfig;
            var isMiner = confParser.IsMiner;

            // Setup ioc 
            var container = SetupIocContainer(isMiner, minerConfig);
        }

        private static IContainer SetupIocContainer(bool isMiner, IMinerConfig minerConf)
        {
            var builder = new ContainerBuilder();

            // Register everything

            // Module registrations
            builder.RegisterModule(new KernelAutofacModule());
            builder.RegisterModule(new SmartContractAutofacModule());
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
            builder.RegisterModule(new MinerAutofacModule(minerConfiguration));

            NodeConfig.Instance.ChainId = chainIdHash.Value.ToByteArray().ToHex();
            builder.RegisterModule(new NodeAutofacModule());

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
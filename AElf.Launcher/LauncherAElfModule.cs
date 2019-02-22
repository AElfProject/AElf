using System.IO;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Kernel;
using AElf.Modularity;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Node;
using AElf.Kernel.Node.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution;
using AElf.OS;
using AElf.OS.Network.Grpc;
using AElf.OS.Node.Application;
using AElf.Runtime.CSharp;
using AElf.RuntimeSetup;
using AElf.Sdk.CSharp.State;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Launcher
{
    [DependsOn(
        typeof(RuntimeSetupAElfModule),
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreMvcModule),
        typeof(CoreOSAElfModule),
        typeof(SmartContractExecutionAElfModule),
        typeof(CSharpRuntimeAElfModule2),
        typeof(DPoSConsensusModule),
        typeof(GrpcNetworkModule),
        typeof(NodeAElfModule))]
    public class LauncherAElfModule : AElfModule
    {
        public static IConfigurationRoot Configuration;

        public ILogger<LauncherAElfModule> Logger { get; set; }

        public LauncherAElfModule()
        {
            Logger = NullLogger<LauncherAElfModule>.Instance;
        }

        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.SetConfiguration(Configuration);
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<GenesisTransactionsGenerator, GenesisTransactionsGenerator>();
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var defaultZero = typeof(BasicContractZero);
            var code = File.ReadAllBytes(defaultZero.Assembly.Location);
            var provider = context.ServiceProvider.GetService<IDefaultContractZeroCodeProvider>();
            provider.DefaultContractZeroRegistration = new SmartContractRegistration()
            {
                Category = 2,
                Code = ByteString.CopyFrom(code),
                CodeHash = Hash.FromRawBytes(code)
            };
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            // TODO: start node

            var eventBus = context.ServiceProvider.GetService<ILocalEventBus>();
            var minerService = context.ServiceProvider.GetService<IMinerService>();
            eventBus.Subscribe<BlockMiningEventData>(eventData => minerService.MineAsync(
                eventData.ChainId, eventData.PreviousBlockHash, eventData.PreviousBlockHeight, eventData.DueTime
            ));
            var chainOptions = context.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value;
            var generator = context.ServiceProvider.GetService<GenesisTransactionsGenerator>();
            var transactions = generator.GetGenesisTransactions(chainOptions.ChainId);
            var dto = new OsBlockchainNodeContextStartDto()
            {
                BlockchainNodeContextStartDto = new BlockchainNodeContextStartDto()
                {
                    ChainId = chainOptions.ChainId,
                    Transactions = transactions
                }
            };
            var osService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
            AsyncHelper.RunSync(async ()=> await osService.StartAsync(dto));
        }

        public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
        {
//            NodeConfiguration confContext = new NodeConfiguration();
//            confContext.LauncherAssemblyLocation = Path.GetDirectoryName(typeof(Node.Node).Assembly.Location);
//
//            var mainChainNodeService = context.ServiceProvider.GetRequiredService<INodeService>();
//            var node = context.ServiceProvider.GetRequiredService<INode>();
//            node.Register(mainChainNodeService);
//            node.Initialize(confContext);
//            node.Start();
        }
    }
}
using AElf.ChainController.Rpc;
using AElf.Consensus.DPoS;
//using AElf.Crosschain;
using AElf.Execution;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Services;
using AElf.Modularity;
using AElf.Net.Rpc;
using AElf.Network;
using AElf.Node;
using AElf.OS;
using AElf.OS.Network.Grpc;
using AElf.Runtime.CSharp;
using AElf.RuntimeSetup;
using AElf.SideChain.Creation;
using AElf.TxPool;
using AElf.Wallet.Rpc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Data;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Modularity;

namespace AElf.Launcher
{
    [DependsOn(
        typeof(RuntimeSetupAElfModule),
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreMvcModule),
        typeof(OSAElfModule),
        typeof(RpcChainControllerAElfModule),
        typeof(ExecutionAElfModule),
        typeof(NetRpcAElfModule),
        typeof(NodeAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(CSharpRuntimeAElfModule2),
        typeof(SideChainAElfModule),
        typeof(RpcWalletAElfModule),
        //typeof(CrossChainAElfModule),
        typeof(NetworkAElfModule),
        typeof(ConsensusKernelAElfModule),
        typeof(DPoSConsensusModule),
        typeof(GrpcNetworkModule),
        typeof(TxPoolAElfModule))]
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
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var connectionStrings = context.ServiceProvider.GetService<IOptions<DbConnectionOptions>>();

            var eventBus = context.ServiceProvider.GetService<ILocalEventBus>();
            var minerService = context.ServiceProvider.GetService<IMinerService>();
            eventBus.Subscribe<BlockMiningEventData>(eventData => minerService.Mine(eventData.ChainId));
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
        }

    }
}
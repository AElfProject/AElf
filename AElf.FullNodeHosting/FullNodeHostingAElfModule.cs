using AElf.ChainController.Rpc;
using AElf.Execution;
using AElf.Kernel.Consensus;
using AElf.Miner;
using AElf.Miner.Rpc;
using AElf.Modularity;
using AElf.Net.Rpc;
using AElf.Network;
using AElf.Node;
using AElf.Runtime.CSharp;
using AElf.RuntimeSetup;
using AElf.SideChain.Creation;
using AElf.Wallet.Rpc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.FullNodeHosting
{
    [DependsOn(
        typeof(RuntimeSetupAElfModule),
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreMvcModule),
        typeof(RpcChainControllerAElfModule),
        typeof(ExecutionAElfModule),
        typeof(MinerAElfModule),
        typeof(NetRpcAElfModule),
        typeof(NodeAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(SideChainAElfModule),
        typeof(RpcWalletAElfModule),
        typeof(MinerRpcAElfModule),
        typeof(NetworkAElfModule),
        typeof(ConsensusKernelAElfModule))]
    public class FullNodeHostingAElfModule : AElfModule
    {
        public static IConfigurationRoot Configuration;
        
        public ILogger<FullNodeHostingAElfModule> Logger { get; set; }

        public FullNodeHostingAElfModule()
        {
            Logger = NullLogger<FullNodeHostingAElfModule>.Instance;
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
            
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
        }

    }
}
using System;
using AElf.Common;
using AElf.CrossChain.Grpc;
using AElf.Kernel;
using AElf.Kernel.Consensus.DPoS;
using AElf.Modularity;
using AElf.OS;
using AElf.OS.Network.Grpc;
using AElf.OS.Rpc.ChainController;
using AElf.OS.Rpc.Net;
using AElf.OS.Rpc.Wallet;
using AElf.Runtime.CSharp;
using AElf.Runtime.CSharp.ExecutiveTokenPlugin;
using AElf.RuntimeSetup;
using AElf.WebApp.Web;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore;
using Volo.Abp.Modularity;

namespace AElf.Blockchains.BasicBaseChain
{
    [DependsOn(
        typeof(DPoSConsensusAElfModule),
        typeof(KernelAElfModule),
        typeof(OSAElfModule),
        typeof(AbpAspNetCoreModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(GrpcNetworkModule),

        //TODO: should move to OSAElfModule
        typeof(ChainControllerRpcModule),
        typeof(WalletRpcModule),
        typeof(NetRpcAElfModule),
        typeof(RuntimeSetupAElfModule),
        typeof(GrpcCrossChainAElfModule),
        
        //web api module
        typeof(WebWebAppAElfModule)
    )]
    public class BasicBaseChainAElfModule : AElfModule<BasicBaseChainAElfModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var config = context.Services.GetConfiguration();
            Configure<ChainOptions>(option =>
            {
                option.ChainId = ChainHelpers.ConvertBase58ToChainId(config["ChainId"]);
            });
        }
    }
}
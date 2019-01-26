using AElf.ChainController.Rpc;
using AElf.Configuration;
using AElf.Crosschain;
using AElf.Cryptography.ECDSA;
using AElf.Database;
using AElf.Kernel.Storages;
using AElf.Modularity;
using AElf.Net.Rpc;
using AElf.Runtime.CSharp;
using AElf.RuntimeSetup;
using AElf.Wallet.Rpc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.AspNetCore.TestBase;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.RPC.Tests
{
    [DependsOn(
        typeof(RpcChainControllerAElfModule),
        typeof(CrosschainAElfModule),
        typeof(NetRpcAElfModule),
        typeof(RpcWalletAElfModule),
        
        typeof(CSharpRuntimeAElfModule),

        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreTestBaseModule)
    )]
    public class TestsRpcAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //TODO: here to generate basic chain data

            context.Services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o=>o.UseInMemoryDatabase());
            context.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o=>o.UseInMemoryDatabase());
            
            //TODO: Remove it
            NodeConfig.Instance.ECKeyPair = new KeyPairGenerator().Generate();
        }
    }
}
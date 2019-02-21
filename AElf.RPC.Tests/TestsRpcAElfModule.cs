using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using AElf.Net.Rpc;
using AElf.Runtime.CSharp;
using AElf.Wallet.Rpc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.AspNetCore.TestBase;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.RPC.Tests
{
    [DependsOn(
        typeof(NetRpcAElfModule),
        typeof(RpcWalletAElfModule),
        typeof(CSharpRuntimeAElfModule2),
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreTestBaseModule)
    )]
    public class TestsRpcAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //TODO: here to generate basic chain data

            Configure<ChainOptions>(o => { o.ChainId = ChainHelpers.ConvertBase58ToChainId("AELF"); });

            context.Services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            context.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
            context.Services.AddTransient<IAccountService>(o => Mock.Of<IAccountService>(
                c => c.GetAccountAsync() == Task.FromResult(Address.FromString("AELF_Test")) && c
                         .VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()) ==
                     Task.FromResult(true)));
        }
    }
}
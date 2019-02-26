using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContractExecution;
using AElf.Modularity;
using AElf.OS.Rpc.ChainController;
using AElf.OS.Rpc.Net;
using AElf.OS.Rpc.Wallet;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.AspNetCore.TestBase;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.OS.Rpc
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreTestBaseModule),
        typeof(CoreKernelAElfModule),
        typeof(KernelAElfModule),
        typeof(SmartContractExecutionAElfModule),
        typeof(ChainControllerRpcModule),
        typeof(WalletRpcModule),
        typeof(NetRpcAElfModule)
    )]
    public class TestBaseRpcAElfModule : AElfModule
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
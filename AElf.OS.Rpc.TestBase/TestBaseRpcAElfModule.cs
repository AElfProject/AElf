using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Node.Application;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution;
using AElf.Modularity;
using AElf.OS.Account;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Node.Application;
using AElf.OS.Rpc.ChainController;
using AElf.OS.Rpc.Net;
using AElf.OS.Rpc.Wallet;
using AElf.OS.Tests;
using AElf.Runtime.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.AspNetCore.TestBase;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.OS.Rpc
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreTestBaseModule),
        typeof(KernelAElfModule),
        typeof(DPoSConsensusAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(ChainControllerRpcModule),
        typeof(WalletRpcModule),
        typeof(NetRpcAElfModule),
        typeof(TestsOSAElfModule),
        typeof(TestBaseKernelAElfModule)
    )]
    public class TestBaseRpcAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ChainOptions>(o => { o.ChainId = ChainHelpers.ConvertBase58ToChainId("AELF"); });
            
            context.Services.AddTransient<ISystemTransactionGenerationService>(o =>
            {
                var mockService = new Mock<ISystemTransactionGenerationService>();
                mockService.Setup(s =>
                    s.GenerateSystemTransactions(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns(new List<Transaction>());
                return mockService.Object;
            });
            
            Mock<IPeerPool> peerPoolMock = new Mock<IPeerPool>();
            peerPoolMock.Setup(p => p.FindPeerByAddress(It.IsAny<string>()))
                .Returns<string>((adr) => null);
            peerPoolMock.Setup(p => p.GetPeers(It.IsAny<bool>()))
                .Returns(new List<IPeer> { });

            context.Services.AddSingleton<IPeerPool>(peerPoolMock.Object);

            context.Services.AddTransient<IBlockExtraDataService>(o =>
            {
                var mockService = new Mock<IBlockExtraDataService>();
                mockService.Setup(s =>
                    s.FillBlockExtraData(It.IsAny<Block>())).Returns(Task.CompletedTask);
                return mockService.Object;
            });
            
            context.Services.AddTransient<IBlockValidationService>(o =>
            {
                var mockService = new Mock<IBlockValidationService>();
                mockService.Setup(s =>
                    s.ValidateBlockBeforeExecuteAsync(It.IsAny<Block>())).Returns(Task.FromResult(true));
                mockService.Setup(s =>
                    s.ValidateBlockAfterExecuteAsync(It.IsAny<Block>())).Returns(Task.FromResult(true));
                return mockService.Object;
            });
        }
        
        // TODO: After the node module refactor, remove it
        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var defaultZero = typeof(BasicContractZero);
            var code = File.ReadAllBytes(defaultZero.Assembly.Location);
            var provider = context.ServiceProvider.GetService<IDefaultContractZeroCodeProvider>();
            provider.DefaultContractZeroRegistration = new SmartContractRegistration
            {
                Category = 2,
                Code = ByteString.CopyFrom(code),
                CodeHash = Hash.FromRawBytes(code)
            };
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var chainId = context.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value.ChainId;
            var account = Address.Parse(context.ServiceProvider.GetService<IOptionsSnapshot<AccountOptions>>()
                .Value.NodeAccount);

            var transactions = RpcTestHelper.GetGenesisTransactions(chainId, account);
            var dto = new OsBlockchainNodeContextStartDto
            {
                BlockchainNodeContextStartDto = new BlockchainNodeContextStartDto
                {
                    ChainId = chainId,
                    Transactions = transactions
                }
            };

            var blockchainNodeContextService = context.ServiceProvider.GetService<IBlockchainNodeContextService>();
            AsyncHelper.RunSync(async () =>
            {
                blockchainNodeContextService.StartAsync(dto.BlockchainNodeContextStartDto);
            });
        }
    }
}
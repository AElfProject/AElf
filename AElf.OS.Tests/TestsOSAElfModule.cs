using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Consensus.DPoS.Tests;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Tests;
using AElf.Modularity;
using AElf.OS.Account;
using AElf.OS.Network.Infrastructure;
using AElf.Runtime.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.Tests
{
    [DependsOn(
        typeof(CoreOSAElfModule),
        typeof(KernelTestAElfModule),
        typeof(DPoSConsensusTestAElfModule)
    )]
    public class TestsOSAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ChainOptions>(o => { o.ChainId = ChainHelpers.ConvertBase58ToChainId("AELF"); });

            var ecKeyPair = CryptoHelpers.GenerateKeyPair();
            var nodeAccount = Address.FromPublicKey(ecKeyPair.PublicKey).GetFormatted();
            var nodeAccountPassword = "123";

            Configure<AccountOptions>(o =>
            {
                o.NodeAccount = nodeAccount;
                o.NodeAccountPassword = nodeAccountPassword;
            });

            Configure<DPoSOptions>(o =>
            {
                var miners = new List<string>();
                for (var i = 0; i < 3; i++)
                {
                    miners.Add(CryptoHelpers.GenerateKeyPair().PublicKey.ToHex());
                }

                o.InitialMiners = miners;
                o.MiningInterval = 4000;
                o.IsBootMiner = true;
            });

            context.Services.AddSingleton<IKeyStore>(o =>
                Mock.Of<IKeyStore>(
                    c => c.OpenAsync(nodeAccount, nodeAccountPassword, false) ==
                         Task.FromResult(AElfKeyStore.Errors.None) &&
                         c.GetAccountKeyPair(nodeAccount) == ecKeyPair)
            );

            context.Services.AddSingleton<IPeerPool>(o =>
            {
                Mock<IPeerPool> peerPoolMock = new Mock<IPeerPool>();
                peerPoolMock.Setup(p => p.FindPeerByAddress(It.IsAny<string>()))
                    .Returns<string>((adr) => null);
                peerPoolMock.Setup(p => p.GetPeers())
                    .Returns(new List<IPeer> { });

                context.Services.AddSingleton<IPeerPool>(peerPoolMock.Object);
                return peerPoolMock.Object;
            });
                        
            context.Services.AddTransient<ISystemTransactionGenerationService>(o =>
            {
                var mockService = new Mock<ISystemTransactionGenerationService>();
                mockService.Setup(s =>
                        s.GenerateSystemTransactions(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns(new List<Transaction>());
                return mockService.Object;
            });

            context.Services.AddTransient<IBlockExtraDataService>(o =>
            {
                var mockService = new Mock<IBlockExtraDataService>();
                mockService.Setup(s =>
                    s.FillBlockExtraData(It.IsAny<BlockHeader>())).Returns(Task.CompletedTask);
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
    }
}
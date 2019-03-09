using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Consensus.DPoS.Tests;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Tests;
using AElf.Modularity;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Node.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.OS
{
    [DependsOn(
        typeof(CoreOSAElfModule),
        typeof(KernelTestAElfModule),
        typeof(DPoSConsensusTestAElfModule)
    )]
    public class OSCoreTestAElfModule : AElfModule
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
            
            context.Services.AddTransient<IAccountService>(o =>
            {
                var mockService = new Mock<IAccountService>();
                mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                    Task.FromResult(CryptoHelpers.SignWithPrivateKey(ecKeyPair.PrivateKey, data)));
                
                mockService.Setup(a => a.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()
                )).Returns<byte[], byte[], byte[]>((signature, data, publicKey) =>
                {
                    var recoverResult = CryptoHelpers.RecoverPublicKey(signature, data, out var recoverPublicKey);
                    return Task.FromResult(recoverResult && publicKey.BytesEqual(recoverPublicKey));
                });

                mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(ecKeyPair.PublicKey);

                return mockService.Object;
            });

            context.Services.AddSingleton<IPeerPool>(o =>
            {
                Mock<IPeerPool> peerPoolMock = new Mock<IPeerPool>();
                peerPoolMock.Setup(p => p.FindPeerByAddress(It.IsAny<string>()))
                    .Returns<string>((adr) => null);
                peerPoolMock.Setup(p => p.GetPeers(It.IsAny<bool>()))
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

            context.Services.AddSingleton<IAElfNetworkServer>(o => Mock.Of<IAElfNetworkServer>());
        }


        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var chainId = context.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value.ChainId;
            var account = Address.Parse(context.ServiceProvider.GetService<IOptionsSnapshot<AccountOptions>>()
                .Value.NodeAccount);

            var info = context.ServiceProvider.GetService<IStaticChainInformationProvider>();

            var dto = new OsBlockchainNodeContextStartDto
            {
                ZeroSmartContract = typeof(BasicContractZero),
                ChainId = chainId,
            };

            dto.InitializationSmartContracts.AddConsensusSmartContract<ConsensusContract>();

            dto.InitializationSmartContracts.AddGenesisSmartContract<TokenContract>();

            var transactions =
                InitChainHelper.GetGenesisTransactions(chainId, account,
                    info.GetSystemContractAddressInGenesisBlock(2));

            dto.InitializationTransactions = transactions;

            var blockchainNodeContextService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
            AsyncHelper.RunSync(() => blockchainNodeContextService.StartAsync(dto));
        }
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Genesis;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Token;
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
        typeof(OSCoreTestAElfModule)
    )]
    public class OSCoreWithChainTestAElfModule : AElfModule
    {
        private OSTestHelper _osTestHelper;

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IPeerPool>(o =>
            {
                Mock<IPeerPool> peerPoolMock = new Mock<IPeerPool>();
                peerPoolMock.Setup(p => p.FindPeerByAddress(It.IsAny<string>()))
                    .Returns<string>((adr) => null);
                peerPoolMock.Setup(p => p.GetPeers(It.IsAny<bool>()))
                    .Returns(new List<IPeer>());
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
            _osTestHelper = context.ServiceProvider.GetService<OSTestHelper>();
            AsyncHelper.RunSync(() => _osTestHelper.MockChain());
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            AsyncHelper.RunSync(() => _osTestHelper.DisposeMock());
        }
    }
}
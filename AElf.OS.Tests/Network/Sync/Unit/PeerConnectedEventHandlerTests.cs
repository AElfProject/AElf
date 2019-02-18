using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Services;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using AElf.OS.Network.Handler;
using AElf.Synchronization.Tests;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.EventBus.Local;
using Xunit;
using Xunit.Abstractions;

namespace AElf.OS.Tests.Network.Sync
{
    public class PeerConnectedEventHandlerTests : OSTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private IBackgroundJobManager _jobManager;
        private ILocalEventBus _eventBus;

        private IOptionsSnapshot<ChainOptions> _optionsMock;

        public PeerConnectedEventHandlerTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;

            _jobManager = GetRequiredService<IBackgroundJobManager>();
            _eventBus = GetRequiredService<ILocalEventBus>();
            
            var optionsMock = new Mock<IOptionsSnapshot<ChainOptions>>();
            optionsMock.Setup(m => m.Value).Returns(new ChainOptions { ChainId = ChainHelpers.DumpBase58(ChainHelpers.GetRandomChainId()) });
            _optionsMock = optionsMock.Object;
        }

        private Mock<INetworkService> MockNetworkService(List<Block> peerChain)
        {
            Mock<INetworkService> mockNetService = new Mock<INetworkService>();

            mockNetService
                .Setup(ns => ns.GetBlockIds(It.IsAny<Hash>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(peerChain.Select(bl => bl.GetHash()).ToList()));
            
            mockNetService
                .Setup(ns => ns.GetBlockByHash(It.IsAny<Hash>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns<Hash, string, bool>((hash, peer, tryOther) => Task.FromResult<IBlock>(peerChain.FirstOrDefault(b => b.GetHash() == hash)));

            return mockNetService;
        }

        private PeerConnectedEventHandler BuildEventHandler(IOptionsSnapshot<ChainOptions> optionsMock,
            IBackgroundJobManager jobManager, INetworkService netService, IFullBlockchainService blockChainService)
        {
            return new PeerConnectedEventHandler
            {
                ChainOptions = optionsMock,
                BackgroundJobManager = jobManager,
                NetworkService = netService,
                BlockchainService = blockChainService
            };
        }
            
        [Fact]
        public async Task Handle_AlreadyKnownHead_ShouldDoNothing()
        {
            var genesis = ChainGenerationHelpers.GetGenesisBlock();
            
            var netMock = MockNetworkService(new List<Block>());
            var backgroundMng = new Mock<IBackgroundJobManager>();
            var mockBlockChainService = new Mock<IFullBlockchainService>();
            mockBlockChainService.Setup(m => m.HasBlockAsync(It.IsAny<int>(), It.IsAny<Hash>())).Returns(Task.FromResult<bool>(true));

            var handler = BuildEventHandler(_optionsMock, backgroundMng.Object, netMock.Object,
                mockBlockChainService.Object);

            await handler.HandleEventAsync(new PeerConnectedEventData {Header = genesis.Header});
            
            // Should not call net or queue job
            backgroundMng.Verify(mock => mock.EnqueueAsync(It.IsAny<object>(), It.IsAny<BackgroundJobPriority>(), It.IsAny<TimeSpan>()), Times.Never);
            netMock.Verify(mock => mock.GetBlockIds(It.IsAny<Hash>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never());
        }
        
        [Fact]
        public async Task Handle_Linked_ShouldRequestBlock()
        {
            // Data
            var genesis = (Block)ChainGenerationHelpers.GetGenesisBlock();
            var block1 = (Block)ChainGenerationHelpers.BuildNext(genesis);
            
            var netMock = MockNetworkService(new List<Block> { genesis, block1 });
            var backgroundMng = new Mock<IBackgroundJobManager>();
            
            var mockBlockChainService = new Mock<IFullBlockchainService>(); // our chain only has genesis
            mockBlockChainService.Setup(m => m.HasBlockAsync(It.IsAny<int>(), It.IsAny<Hash>()))
                .Returns<int, Hash>( (chainId, blockId) => Task.FromResult(blockId == genesis.GetHash()));

            var handler = BuildEventHandler(_optionsMock, backgroundMng.Object, netMock.Object,
                mockBlockChainService.Object);
            
            await handler.HandleEventAsync(new PeerConnectedEventData { Header = block1.Header }); // Block 2 is what we receive
            
            // Should not call net or queue job
            backgroundMng.Verify(mock => mock.EnqueueAsync(It.IsAny<object>(), It.IsAny<BackgroundJobPriority>(), It.IsAny<TimeSpan>()), Times.Never);
            netMock.Verify(mock => mock.GetBlockByHash(It.Is<Hash>(h => h == block1.GetHash()), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }
    }
}
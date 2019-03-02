using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Handlers;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using AElf.Synchronization.Tests;
using Microsoft.Extensions.Options;
using Moq;
using NSubstitute;
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

        private IOptionsSnapshot<ChainOptions> chainOptions;

        public PeerConnectedEventHandlerTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;

            _jobManager = GetRequiredService<IBackgroundJobManager>();
            _eventBus = GetRequiredService<ILocalEventBus>();
            
            var optionsMock = new Mock<IOptionsSnapshot<ChainOptions>>();
            optionsMock.Setup(m => m.Value).Returns(new ChainOptions { ChainId = ChainHelpers.GetRandomChainId() });
            chainOptions = optionsMock.Object;
        }

        private Mock<INetworkService> MockNetworkService(List<Block> peerChain, int requestIdSize = 2)
        {
            Mock<INetworkService> mockNetService = new Mock<INetworkService>();

            mockNetService
                .Setup(ns => ns.GetBlockIdsAsync(It.IsAny<Hash>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns<Hash, int, string>((hash, count, _) =>
                {
                    var topBlock = peerChain.First(b => b.GetHash() == hash);
                    var toGet = peerChain
                        .Where(b => b.Height < topBlock.Height)
                        .OrderByDescending(b => b.Height)
                        .Take(requestIdSize)
                        .Select(b => b.GetHash())
                        .ToList();
                    
                    return Task.FromResult(toGet);
                });
            
            mockNetService
                .Setup(ns => ns.GetBlockByHashAsync(It.IsAny<Hash>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns<Hash, string, bool>((hash, peer, tryOther) => Task.FromResult<IBlock>(peerChain.FirstOrDefault(b => b.GetHash() == hash)));

            return mockNetService;
        }

        private PeerConnectedEventHandler BuildEventHandler(IOptionsSnapshot<NetworkOptions> netOptions, IOptionsSnapshot<ChainOptions> optionsMock,
            IBackgroundJobManager jobManager, INetworkService netService, IBlockchainService blockChainService)
        {
            return new PeerConnectedEventHandler
            {
                NetworkOptions = netOptions,
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

            var handler = BuildEventHandler(null, chainOptions, backgroundMng.Object, netMock.Object,
                mockBlockChainService.Object);

            await handler.HandleEventAsync(new PeerConnectedEventData {Header = genesis.Header});
            
            // Should not call net or queue job
            backgroundMng.Verify(mock => mock.EnqueueAsync(It.IsAny<object>(), It.IsAny<BackgroundJobPriority>(), It.IsAny<TimeSpan>()), Times.Never);
            netMock.Verify(mock => mock.GetBlockIdsAsync(It.IsAny<Hash>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never());
        }
        
        [Fact(Skip = "Scenario has changed")]
        public async Task Handle_Linked_ShouldRequestBlock()
        {
            var genesis = (Block)ChainGenerationHelpers.GetGenesisBlock();
            var block2 = (Block)ChainGenerationHelpers.BuildNext(genesis);
            
            var netMock = MockNetworkService(new List<Block> { genesis, block2 });
            var backgroundMng = new Mock<IBackgroundJobManager>();
            
            var mockBlockChainService = new Mock<IFullBlockchainService>(); // our chain only has genesis
            mockBlockChainService.Setup(m => m.HasBlockAsync(It.IsAny<int>(), It.IsAny<Hash>()))
                .Returns<int, Hash>( (chainId, blockId) => Task.FromResult(blockId == genesis.GetHash()));

            var handler = BuildEventHandler(null, chainOptions, backgroundMng.Object, netMock.Object,
                mockBlockChainService.Object);
            
            await handler.HandleEventAsync(new PeerConnectedEventData { Header = block2.Header }); // Block 2 is what we receive
            
            // Should not queue job but will request the missing block
            backgroundMng.Verify(mock => mock.EnqueueAsync(It.IsAny<object>(), It.IsAny<BackgroundJobPriority>(), It.IsAny<TimeSpan>()), Times.Never);
            netMock.Verify(mock => mock.GetBlockByHashAsync(It.Is<Hash>(h => h == block2.GetHash()), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact(Skip = "Scenario has changed")]
        public async Task Handle_IdRequestCountExceedsMissingBlocks_ShouldRequestAgain()
        {
            // setup: G + [B2,B6], B2 is already known by the node and id request size is set to 2.
            // expected: 2 chunks will be requested: [B5,B4] and [B3,B2]. It will match on B2 and will
            // queue for download [B3,B6]
            
            var genesis = (Block)ChainGenerationHelpers.GetGenesisBlock();
            
            List<Block> blocks = new List<Block> { genesis };
            
            var current = genesis;
            for (int i = 0; i < 5; i++)
            {
                current = (Block) ChainGenerationHelpers.BuildNext(current);
                blocks.Add(current);
            }

            // set request chunk to 2
            var optionsMock = new Mock<IOptionsSnapshot<NetworkOptions>>();
            optionsMock.Setup(m => m.Value).Returns(new NetworkOptions { BlockIdRequestCount = 2 });
            var netOptions = optionsMock.Object;
            
            // the peers has all the blocks
            var netMock = MockNetworkService(blocks); 
            
            // our chain only has genesis and B1
            var mockBlockChainService = new Mock<IFullBlockchainService>(); 
            mockBlockChainService.Setup(m => m.HasBlockAsync(It.IsAny<int>(), It.IsAny<Hash>()))
                .Returns<int, Hash>( (chainId, blockId) => 
                    Task.FromResult(blockId == genesis.GetHash() || blockId == blocks[1].GetHash()));
            
            var backgroundMng = new Mock<IBackgroundJobManager>();
            
            var handler = BuildEventHandler(netOptions, chainOptions, backgroundMng.Object, netMock.Object,
                mockBlockChainService.Object);
            
            // B6 is what we receive
            await handler.HandleEventAsync(new PeerConnectedEventData { Header = blocks[5].Header });
            
            netMock.Verify(mock => mock.GetBlockIdsAsync(It.IsAny<Hash>(), It.IsAny<int>(), It.IsAny<string>()), Times.Exactly(2));
            
            backgroundMng.Verify(l => l.EnqueueAsync(It.IsAny<object>(), It.IsAny<BackgroundJobPriority>(), It.IsAny<TimeSpan?>()), Times.Once);
        }
    }
}
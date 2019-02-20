using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Jobs;
using AElf.OS.Network;
using AElf.Synchronization.Tests;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.EventBus.Local;
using Xunit;
using Xunit.Abstractions;

namespace AElf.OS.Tests.Network.Sync
{
    public class SyncTester : OSTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IBackgroundJobManager _jobManager;
        private readonly ILocalEventBus _eventBus;

        public SyncTester(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;

            _jobManager = GetRequiredService<IBackgroundJobManager>();
            _eventBus = GetRequiredService<ILocalEventBus>();
        }
        
        [Fact]
        public void Test()
        {
            
            List<Block> downloadedBlocks = new List<Block>();
            List<IBlock> initBlocks = new List<IBlock>();
            
            var genesis = ChainGenerationHelpers.GetGenesisBlock();
            var block1 = ChainGenerationHelpers.BuildNext(genesis);
            var block2 = ChainGenerationHelpers.BuildNext(block1);
            
            initBlocks.Add(genesis);
            initBlocks.Add(block1);
            initBlocks.Add(block2);
            
            Mock<INetworkService> _mockNetService = new Mock<INetworkService>();

            _mockNetService
                .Setup(ns => ns.GetBlockIdsAsync(It.IsAny<Hash>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.FromResult(initBlocks.Select(bl => bl.GetHash()).ToList()));
            
            _mockNetService
                .Setup(ns => ns.GetBlockByHashAsync(It.IsAny<Hash>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns<Hash, string, bool>((hash, peer, tryOthers) => Task.FromResult(initBlocks.FirstOrDefault(b => b.GetHash() == hash)));
            
            Mock<IFullBlockchainService> blockchainService = new Mock<IFullBlockchainService>();
            blockchainService
                .Setup(bls => bls.AddBlockAsync(It.IsAny<int>(), It.IsAny<Block>()))
                .Returns<int, Block>((chainId, block) => Task.Run(() => downloadedBlocks.Add(block)));

            blockchainService
                .Setup(bcs => bcs.GetChainAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(new Chain()));

            blockchainService
                .Setup(bcs => bcs.HasBlockAsync(It.IsAny<int>(), It.IsAny<Hash>()))
                .Returns(Task.FromResult(false)); // this service never has the block
            
            var optionsMock = new Mock<IOptionsSnapshot<ChainOptions>>();
            optionsMock.Setup(m => m.Value).Returns(new ChainOptions { ChainId = ChainHelpers.DumpBase58(ChainHelpers.GetRandomChainId()) });
            
            // Network layer and service is mocked
//            _eventBus.Subscribe<PeerConnectedEventData>(args =>
//            {
//                var h = new ForkDownloadJob();
//                h.NetworkService = _mockNetService.Object;
//                h.BlockchainService = blockchainService.Object;
//                
//                _jobManager.EnqueueAsync(h);
//
//                return Task.FromResult(true);
//            });
            
            // Simulate the connection
            //_eventBus.PublishAsync(new PeerConnectedEventData { BlockId = block2.GetHash() });
            
            var h = new ForkDownloadJob();
            h.NetworkService = _mockNetService.Object;
            h.BlockchainService = blockchainService.Object;
            h.ChainOptions = optionsMock.Object;
                
            //_jobManager.EnqueueAsync(h);
            h.Execute(new ForkDownloadJobArgs { BlockHashes = initBlocks.Select(bl => bl.GetHash()).ToList() });
            
            // setup some blocks to get
            // mock network service
            
            // setup a receiver list and an initial list.
            // objective is to fill the receiver with the same as the initial (everything as been requested)
            ;
        }
    }
}
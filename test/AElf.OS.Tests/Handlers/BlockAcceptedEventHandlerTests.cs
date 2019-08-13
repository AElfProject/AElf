using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.OS.Handlers.AElf.OS.Network.Handler;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using Xunit;

namespace AElf.OS.Handlers
{
    public class BlockAcceptedEventHandlerTests : BlockSyncTestBase
    {
        private readonly BlockAcceptedEventHandler _blockAcceptedEventHandler;
        private readonly IBlockchainService _blockchainService;
        private readonly INodeSyncStateProvider _nodeSyncStateProvider;
        private readonly OSTestHelper _osTestHelper;

        public BlockAcceptedEventHandlerTests()
        {
            _blockAcceptedEventHandler = GetRequiredService<BlockAcceptedEventHandler>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _nodeSyncStateProvider = GetRequiredService<INodeSyncStateProvider>();
        }

        [Fact]
        public async Task SyncFinished_BlockAccept_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var block = _osTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight);
            var eventData = new BlockAcceptedEvent
            {
                BlockHeader = block.Header,
                HasFork = false
            };
            _nodeSyncStateProvider.SetSyncTarget(-1);
            await _blockAcceptedEventHandler.HandleEventAsync(eventData);
        }
        
        [Fact]
        public async Task Syncing_BlockAccept_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var block = _osTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight);
            var eventData = new BlockAcceptedEvent
            {
                BlockHeader = block.Header,
                HasFork = false
            };
            _nodeSyncStateProvider.SetSyncTarget(1);
            await _blockAcceptedEventHandler.HandleEventAsync(eventData);
        }
    }
}
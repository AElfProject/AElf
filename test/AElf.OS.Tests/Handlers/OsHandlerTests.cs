using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.OS.Handlers.AElf.OS.Network.Handler;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using Google.Protobuf;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;
using BlockAcceptedEventHandler = AElf.OS.Handlers.AElf.OS.Network.Handler.BlockAcceptedEventHandler;

namespace AElf.OS.Handlers
{
    public class OsHandlerTests : BlockSyncTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISyncStateService _syncStateService;
        private readonly OSTestHelper _osTestHelper;
        private readonly BlockMinedEventHandler _blockMinedEventHandler;
        private readonly BlockAcceptedEventHandler _blockAcceptedEventHandler;
        private readonly BlockReceivedEventHandler _blockReceivedEventHandler;
        private readonly PeerConnectedEventHandler _peerConnectedEventHandler;
        private readonly ILocalEventBus _eventBus;

        public OsHandlerTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _syncStateService = GetRequiredService<ISyncStateService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _blockMinedEventHandler = GetRequiredService<BlockMinedEventHandler>();
            _blockAcceptedEventHandler = GetRequiredService<BlockAcceptedEventHandler>();
            _blockReceivedEventHandler = GetRequiredService<BlockReceivedEventHandler>();
            _peerConnectedEventHandler = GetRequiredService<PeerConnectedEventHandler>();
            _eventBus = GetRequiredService<ILocalEventBus>();
        }

        [Fact]
        public async Task BlockMined_HandleEventAsync_Test()
        {
            await _syncStateService.StartSyncAsync();

            var chain = await _blockchainService.GetChainAsync();
            var tx = await _osTestHelper.GenerateTransferTransaction();
            await _blockchainService.AddTransactionsAsync(new[] {tx});
            var block = _osTestHelper.GenerateBlock(chain.LongestChainHash,
                chain.LongestChainHeight, new[] {tx});
            await _blockchainService.AddBlockAsync(block);
            await _blockchainService.AttachBlockToChainAsync(chain, block);
            var minedEventData = new BlockMinedEventData
            {
                BlockHeader = block.Header
            };
            await _blockMinedEventHandler.HandleEventAsync(minedEventData);
        }

        [Fact]
        public async Task BlockReceived_HandlerEventAsync_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var tx = await _osTestHelper.GenerateTransferTransaction();
            await _blockchainService.AddTransactionsAsync(new[] {tx});
            var block = _osTestHelper.GenerateBlock(chain.LongestChainHash,
                chain.LongestChainHeight, new[] {tx});
            var blockWithTransactions = new BlockWithTransactions
            {
                Header = block.Header,
                Transactions = {tx}
            };
            var pubkey = block.Header.SignerPubkey.ToHex();
            await _blockReceivedEventHandler.HandleEventAsync(
                new BlockReceivedEvent(blockWithTransactions, pubkey));
        }

        [Fact]
        public async Task BlockAccepted_HandlerEventAsync_Test()
        {
            await _syncStateService.StartSyncAsync();

            var chain = await _blockchainService.GetChainAsync();
            var tx = await _osTestHelper.GenerateTransferTransaction();
            await _blockchainService.AddTransactionsAsync(new[] {tx});
            var block = _osTestHelper.GenerateBlock(chain.LongestChainHash,
                chain.LongestChainHeight, new[] {tx});

            BlockAcceptedEvent eventData = null;
            _eventBus.Subscribe<BlockAcceptedEvent>(ed =>
            {
                eventData = ed;
                return Task.CompletedTask;
            });
            await _blockAcceptedEventHandler.HandleEventAsync(new BlockAcceptedEvent
            {
                Block = block
            });
        }

        [Fact]
        public async Task PeerConnectedEventHandler_Test()
        {
            AnnouncementReceivedEventData eventData = null;
            _eventBus.Subscribe<AnnouncementReceivedEventData>(ed =>
            {
                eventData = ed;
                return Task.CompletedTask;
            });
            var chain = await _blockchainService.GetChainAsync();
            var nodeInfo = new NodeInfo {Endpoint = "127.0.0.1:8000", Pubkey = ByteString.CopyFromUtf8("public-key")};
            await _peerConnectedEventHandler.HandleEventAsync(new PeerConnectedEventData(nodeInfo, chain.BestChainHash,
                chain.BestChainHeight));
            eventData.ShouldNotBeNull();
        }
    }
}
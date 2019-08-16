using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.OS.Handlers.Network
{
    public class PeerConnectedEventHandlerTests : BlockSyncTestBase
    {
        private readonly PeerConnectedEventHandler _peerConnectedEventHandler;
        private readonly IBlockchainService _blockchainService;

        public PeerConnectedEventHandlerTests()
        {
            _peerConnectedEventHandler = GetRequiredService<PeerConnectedEventHandler>();
            _blockchainService = GetRequiredService<IBlockchainService>();
        }

        [Fact]
        public async Task PeerConnectedEventHandler_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var blockHeader = await _blockchainService.GetBlockHeaderByHashAsync(chain.BestChainHash);
            var nodeInfo = new NodeInfo
            {
                Endpoint = "http://127.0.0.1:1000",
                Pubkey = ByteString.CopyFromUtf8("pubkey")
            };
            var eventData = new PeerConnectedEventData(nodeInfo, blockHeader);
            var result = _peerConnectedEventHandler.HandleEventAsync(eventData);
            result.Status.ShouldBe(TaskStatus.RanToCompletion);
        }
    }
}
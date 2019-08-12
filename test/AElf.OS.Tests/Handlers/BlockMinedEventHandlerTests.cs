using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Handlers.AElf.OS.Network.Handler;
using AElf.OS.Network.Events;
using AElf.Types;
using Google.Protobuf;
using Xunit;

namespace AElf.OS.Handlers
{
    public class BlockMinedEventHandlerTests : BlockSyncTestBase
    {
        private readonly BlockReceivedEventHandler _blockReceivedEventHandler;
        private readonly BlockMinedEventHandler _blockMinedEventHandler;
        private readonly IBlockchainService _blockchainService;
        private readonly OSTestHelper _osTestHelper;
        private readonly IAccountService _accountService;
        
        public BlockMinedEventHandlerTests()
        {
            _blockReceivedEventHandler = GetRequiredService<BlockReceivedEventHandler>();
            _blockMinedEventHandler = GetRequiredService<BlockMinedEventHandler>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _accountService = GetRequiredService<IAccountService>();
        }

        [Fact]
        public async Task BlockMined_InvalidData_Test()
        {
            //without header info
            var eventData = new BlockMinedEventData
            {
                HasFork = false
            };
            await _blockMinedEventHandler.HandleEventAsync(eventData);
            
            //not exist block
            var block = _osTestHelper.GenerateBlock(Hash.FromString("invalid"), 20, null);
            eventData = new BlockMinedEventData
            {
                BlockHeader = block.Header,
                HasFork = false
            };
            await _blockMinedEventHandler.HandleEventAsync(eventData);
        }

        [Fact]
        public async Task BlockMined_ValidData_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var blockWithTransactions =
                _osTestHelper.GenerateBlockWithTransactions(chain.BestChainHash, chain.BestChainHeight);
            var publicKey = await _accountService.GetPublicKeyAsync();
            var receivedEvent = new BlockReceivedEvent(blockWithTransactions, ByteString.CopyFrom(publicKey).ToHex());
            await _blockReceivedEventHandler.HandleEventAsync(receivedEvent);
            
            var eventData = new BlockMinedEventData
            {
                BlockHeader = blockWithTransactions.Header,
                HasFork = false
            };
            await _blockMinedEventHandler.HandleEventAsync(eventData);
        }
    }
}
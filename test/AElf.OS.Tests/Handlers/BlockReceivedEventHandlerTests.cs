using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.OS.Handlers
{
    public class BlockReceivedEventHandlerTests : BlockSyncTestBase
    {
        private readonly BlockReceivedEventHandler _blockReceivedEventHandler;
        private readonly OSTestHelper _osTestHelper;
        private readonly IBlockchainService _chainService;
        private readonly IAccountService _accountService;

        public BlockReceivedEventHandlerTests()
        {
            _blockReceivedEventHandler = GetRequiredService<BlockReceivedEventHandler>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _chainService = GetRequiredService<IBlockchainService>();
            _accountService = GetRequiredService<IAccountService>();
        }

        [Fact]
        public async Task BlockReceivedEvent_NormalBlock_Test()
        {
            //prepare new block data
            var chain = await _chainService.GetChainAsync();
            var transactions = await _osTestHelper.GenerateTransferTransactions(3);
            var blockWithTransactions =
                _osTestHelper.GenerateBlockWithTransactions(chain.BestChainHash, chain.BestChainHeight, transactions.ToList());
            
            var publicKey = await _accountService.GetPublicKeyAsync();
            var eventData = new BlockReceivedEvent(blockWithTransactions, ByteString.CopyFrom(publicKey).ToHex());
            var blockHash = blockWithTransactions.GetHash();
            
            //call handler
            await _blockReceivedEventHandler.HandleEventAsync(eventData);
            
            //verify
            var queryResult = await _chainService.GetBlockByHashAsync(blockHash);
            queryResult.ShouldNotBeNull();
            queryResult.TransactionIds.Count().ShouldBe(3);
        }
        
        [Fact]
        public async Task BlockReceivedEvent_InvalidBlock_Test()
        {
            //prepare new block data
            var transactions = await _osTestHelper.GenerateTransferTransactions(5);
            var blockWithTransactions =
                _osTestHelper.GenerateBlockWithTransactions(Hash.FromString("invalid"), 100, transactions.ToList());
            var publicKey = await _accountService.GetPublicKeyAsync();
            var eventData = new BlockReceivedEvent(blockWithTransactions, ByteString.CopyFrom(publicKey).ToHex());
            var blockHash = blockWithTransactions.GetHash();
            
            //call handler
            await _blockReceivedEventHandler.HandleEventAsync(eventData);
            
            //verify
            var queryResult = await _chainService.GetBlockByHashAsync(blockHash);
            queryResult.ShouldBeNull();
        }
    }
}
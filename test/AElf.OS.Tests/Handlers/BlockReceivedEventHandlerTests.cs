using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.BlockSync.Domain;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using Shouldly;
using Xunit;

namespace AElf.OS.Handlers
{
    public class BlockReceivedEventHandlerTests : OSTestBase
    {
        private readonly BlockReceivedEventHandler _blockReceivedEventHandler;
        private readonly IBlockDownloadJobStore _blockDownloadJobStore;
        private readonly OSTestHelper _osTestHelper;

        public BlockReceivedEventHandlerTests()
        {
            _blockDownloadJobStore = GetRequiredService<IBlockDownloadJobStore>();
            _blockReceivedEventHandler = GetRequiredService<BlockReceivedEventHandler>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
        }

        [Fact]
        public async Task HandleEvent_Test()
        {
            var block = new BlockWithTransactions
            {
                Header = _osTestHelper.GenerateBlock(HashHelper.ComputeFrom("BlockHash"), 100).Header
            };

            var eventData = new BlockReceivedEvent(block, block.Header.SignerPubkey.ToHex());
            
            await HandleEventAsync(eventData);
            var job = await _blockDownloadJobStore.GetFirstWaitingJobAsync();
            job.ShouldNotBeNull();
            job.TargetBlockHash.ShouldBe(block.GetHash());
            job.TargetBlockHeight.ShouldBe(block.Height);
            job.SuggestedPeerPubkey.ShouldBe(block.Header.SignerPubkey.ToHex());

            eventData = new BlockReceivedEvent(block, "InvalidPubkey");
            await HandleEventAsync(eventData);
            job = await _blockDownloadJobStore.GetFirstWaitingJobAsync();
            job.ShouldBeNull();
        }
        
        private async Task HandleEventAsync(BlockReceivedEvent eventData)
        {
            await _blockReceivedEventHandler.HandleEventAsync(eventData);
            await Task.Delay(500);
        }
    }
}
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.OS.Handlers.Network
{
    public class AnnouncementReceivedEventHandlerTests : BlockSyncTestBase
    {
        private readonly AnnouncementReceivedEventHandler _announcementReceivedEventHandler;
        private readonly IBlockchainService _blockchainService;
        private readonly OSTestHelper _osTestHelper;
        private readonly IAccountService _accountService;

        public AnnouncementReceivedEventHandlerTests()
        {
            _announcementReceivedEventHandler = GetRequiredService<AnnouncementReceivedEventHandler>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _accountService = GetRequiredService<IAccountService>();
        }

        [Fact]
        public async Task AnnouncementReceivedHandler_ValidBlockEvent_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var block = _osTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight);
            var announce = new BlockAnnouncement
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height,
                HasFork = false
            };
            var sendKey = ByteString.CopyFrom(await _accountService.GetPublicKeyAsync()).ToHex();
            var eventData = new AnnouncementReceivedEventData(announce, sendKey);

            var result = _announcementReceivedEventHandler.HandleEventAsync(eventData);
            result.Status.ShouldBe(TaskStatus.RanToCompletion);
        }
        
        [Fact]
        public async Task AnnouncementReceivedHandler_InValidBlockEvent_Test()
        {
            var block = _osTestHelper.GenerateBlock(Hash.FromString("invalid"), 1);
            var announce = new BlockAnnouncement
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height,
                HasFork = false
            };
            var sendKey = ByteString.CopyFrom(await _accountService.GetPublicKeyAsync()).ToHex();
            var eventData = new AnnouncementReceivedEventData(announce, sendKey);

            var result = _announcementReceivedEventHandler.HandleEventAsync(eventData);
            result.Status.ShouldBe(TaskStatus.RanToCompletion);
        }
    }
}
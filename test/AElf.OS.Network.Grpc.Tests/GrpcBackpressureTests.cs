using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class GrpcBackpressureTests : GrpcBackpressureTestBase
    {
        private readonly IPeer _peerUnderTest;

        public GrpcBackpressureTests()
        {
            _peerUnderTest = GetRequiredService<IPeer>();
        }

        [Fact]
        public void EnqueueAnnouncement_ShouldDrop_IfBufferFull()
        {
            // fill the buffer
            for (int i = 0; i < NetworkConstants.DefaultMaxBufferedAnnouncementCount; i++)
                _peerUnderTest.EnqueueAnnouncement(new BlockAnnouncement(), null);
            
            var overflowException = Assert.Throws<NetworkException>(()=> _peerUnderTest.EnqueueAnnouncement(new BlockAnnouncement(), null));
            overflowException.ExceptionType.ShouldBe(NetworkExceptionType.FullBuffer);
        }

        [Fact]
        public void EnqueueTransaction_ShouldDrop_IfBufferFull()
        {
            // fill the buffer
            for (int i = 0; i < NetworkConstants.DefaultMaxBufferedTransactionCount; i++)
                _peerUnderTest.EnqueueTransaction(new Transaction(), null);
            
            var overflowException = Assert.Throws<NetworkException>(()=> _peerUnderTest.EnqueueTransaction(new Transaction(), null));
            overflowException.ExceptionType.ShouldBe(NetworkExceptionType.FullBuffer);
        }
        
        [Fact]
        public void EnqueueBlock_ShouldDrop_IfBufferFull()
        {
            // fill the buffer
            for (int i = 0; i < NetworkConstants.DefaultMaxBufferedBlockCount; i++)
                _peerUnderTest.EnqueueBlock(new BlockWithTransactions(), null);
            
            var overflowException = Assert.Throws<NetworkException>(()=> _peerUnderTest.EnqueueBlock(new BlockWithTransactions(), null));
            overflowException.ExceptionType.ShouldBe(NetworkExceptionType.FullBuffer);
        }
    }
}
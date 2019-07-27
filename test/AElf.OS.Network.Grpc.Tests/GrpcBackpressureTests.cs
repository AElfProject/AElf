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
            NetworkException callbackException = null;
            void ErrorCallback(NetworkException networkException) => callbackException = networkException;

            for (int i = 0; i <= NetworkConstants.DefaultMaxBufferedAnnouncementCount; i++)
            {
                _peerUnderTest.EnqueueAnnouncement(new BlockAnnouncement(), ErrorCallback);
                callbackException.ShouldBeNull();
            }
            
            _peerUnderTest.EnqueueAnnouncement(new BlockAnnouncement(), ErrorCallback);
            callbackException.ShouldNotBeNull();
        }

        [Fact]
        public void EnqueueTransaction_ShouldDrop_IfBufferFull()
        {
            NetworkException callbackException = null;
            void ErrorCallback(NetworkException networkException) => callbackException = networkException;

            for (int i = 0; i <= NetworkConstants.DefaultMaxBufferedTransactionCount; i++)
            {
                _peerUnderTest.EnqueueTransaction(new Transaction(), ErrorCallback);
                callbackException.ShouldBeNull();
            }
            
            _peerUnderTest.EnqueueTransaction(new Transaction(), ErrorCallback);
            callbackException.ShouldNotBeNull();
        } 

        [Fact]
        public void EnqueueBlock_ShouldDrop_IfBufferFull()
        {
            NetworkException callbackException = null;
            void ErrorCallback(NetworkException networkException) => callbackException = networkException;

            for (int i = 0; i <= NetworkConstants.DefaultMaxBufferedBlockCount; i++)
            {
                _peerUnderTest.EnqueueBlock(new BlockWithTransactions(), ErrorCallback);
                callbackException.ShouldBeNull();
            }
            
            _peerUnderTest.EnqueueBlock(new BlockWithTransactions(), ErrorCallback);
            callbackException.ShouldNotBeNull();
        } 
    }
}
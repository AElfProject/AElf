using System;
using System.Threading;
using AElf.Network.Connection;
using AElf.Network.Peers;
using Moq;
using Xunit;

namespace AElf.Network.Tests.NetworkManagerTests
{
    public class TimeoutRequestTests
    {
        [Fact]
        public void TryPeer_FireWithNullPeer_ThrowsException()
        {
            TimeoutRequest request = new TimeoutRequest(0, null, 1000);
            var ex = Assert.Throws<InvalidOperationException>(() => request.TryPeer(null));
            Assert.Equal("Peer cannot be null.", ex.Message);
        }
        
        [Fact]
        public void TryPeer_ShouldEnqueueMessage()
        {
            Mock<IPeer> fakePeer = new Mock<IPeer>();
            fakePeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>(), null));
            
            TimeoutRequest request = new TimeoutRequest(0, new Message(), 1000);
            request.TryPeer(fakePeer.Object);
            
            fakePeer.Verify(mock => mock.EnqueueOutgoing(It.IsAny<Message>(), null), Times.Once());
        }

        [Fact]
        public void TryPeer_TimoutEvent_ShouldFire()
        {
            Mock<IPeer> fakePeer = new Mock<IPeer>();
            fakePeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>(), null));
            
            TimeoutRequest request = new TimeoutRequest(0, new Message(), 1);

            bool wasFired = false;
            request.RequestTimedOut += (sender, args) => wasFired = true;
            
            request.TryPeer(fakePeer.Object);
            
            // Wait longer than the timout
            Thread.Sleep(50); 
            
            Assert.True(wasFired);
        }
        
        [Fact]
        public void TryPeer_WaitLongerThanTimout_ShouldNotFire()
        {
            Mock<IPeer> fakePeer = new Mock<IPeer>();
            fakePeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>(), null));
            
            TimeoutRequest request = new TimeoutRequest(0, new Message(), 50);

            bool wasFired = false;
            request.RequestTimedOut += (sender, args) => wasFired = true;
            
            request.TryPeer(fakePeer.Object);
            
            // Wait for less time than than the timeout
            Thread.Sleep(10);
            
            Assert.False(wasFired);
        }

        [Fact]
        public void TryPeer_AfterTimeout_HasTimeoutShouldBeTrue()
        {
            Mock<IPeer> fakePeer = new Mock<IPeer>();
            fakePeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>(), null));
            
            TimeoutRequest request = new TimeoutRequest(0, new Message(), 5);
            request.TryPeer(fakePeer.Object);
            
            Thread.Sleep(20);
            
            Assert.True(request.HasTimedOut);
        }
        
        [Fact]
        public void TryPeer_BeforeTimeout_HasTimeoutShouldBeFalse()
        {
            Mock<IPeer> fakePeer = new Mock<IPeer>();
            fakePeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>(), null));
            
            TimeoutRequest request = new TimeoutRequest(0, new Message(), 20);
            request.TryPeer(fakePeer.Object);
            
            Assert.False(request.HasTimedOut);
        }
        
        [Fact]
        public void TryPeer_RetryDuringRequest_ShouldThrowException()
        {
            Mock<IPeer> fakePeer = new Mock<IPeer>();
            fakePeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>(), null));
            
            TimeoutRequest request = new TimeoutRequest(0, new Message(), 20);
            request.TryPeer(fakePeer.Object);
            
            var ex = Assert.Throws<InvalidOperationException>(() => request.TryPeer(fakePeer.Object));
            Assert.Equal("Cannot switch peer before timeout.", ex.Message);
        }
        
        [Fact]
        public void TryPeer_ExceedRetry_ShouldThrowException()
        {
            Mock<IPeer> fakePeer = new Mock<IPeer>();
            fakePeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>(), null));
            
            TimeoutRequest request = new TimeoutRequest(0, new Message(), 1);
            request.MaxRetryCount = 1;
            
            request.TryPeer(fakePeer.Object);
            
            Thread.Sleep(10);
            
            var ex = Assert.Throws<InvalidOperationException>(() => request.TryPeer(fakePeer.Object));
            Assert.Equal("Cannot retry : max retry count reached.", ex.Message);
        }
    }
}
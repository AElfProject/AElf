using System;
using System.Threading;
using AElf.Network.Connection;
using AElf.Network.Peers;
using Moq;
using Xunit;

namespace AElf.Network.Tests.NetworkManager
{
    public class TimeoutRequestTests
    {
        [Fact]
        public void TryPeer_ShouldEnqueueMessage()
        {
            Mock<IPeer> fakePeer = new Mock<IPeer>();
            fakePeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>()));
            
            Message msg = new Message();
            
            TimeoutRequest request = new TimeoutRequest(new byte[0], msg, 1000);
            request.TryPeer(fakePeer.Object);
            
            fakePeer.Verify(mock => mock.EnqueueOutgoing(It.IsAny<Message>()), Times.Once());
        }
        
        [Fact]
        public void TryPeer_FireWithNullPeer_ThrowsException()
        {
            TimeoutRequest request = new TimeoutRequest(new byte[0], null, 1000);
            var ex = Assert.Throws<InvalidOperationException>(() => request.TryPeer(null));
            Assert.Equal(ex.Message, "Peer cannot be null.");
        }
        
        [Fact]
        public void TryPeer_FireWithNullMessage_ThrowsException()
        {
            TimeoutRequest request = new TimeoutRequest(new byte[0], null, 1000);
            var ex = Assert.Throws<InvalidOperationException>(() => request.TryPeer(new Peer(1234)));
            Assert.Equal(ex.Message, "RequestMessage cannot be null.");
        }

        [Fact]
        public void TryPeer_TimoutEvent_ShouldFire()
        {
            Mock<IPeer> fakePeer = new Mock<IPeer>();
            fakePeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>()));
            
            Message msg = new Message();
            
            TimeoutRequest request = new TimeoutRequest(new byte[0], msg, 1);

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
            fakePeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>()));
            
            Message msg = new Message();
            
            TimeoutRequest request = new TimeoutRequest(new byte[0], msg, 50);

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
            fakePeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>()));
            
            Message msg = new Message();
            
            TimeoutRequest request = new TimeoutRequest(new byte[0], msg, 5);
            request.TryPeer(fakePeer.Object);
            
            Thread.Sleep(20);
            
            Assert.True(request.HasTimedOut);
        }
        
        [Fact]
        public void TryPeer_BeforeTimeout_HasTimeoutShouldBeFalse()
        {
            Mock<IPeer> fakePeer = new Mock<IPeer>();
            fakePeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>()));
            
            Message msg = new Message();
            
            TimeoutRequest request = new TimeoutRequest(new byte[0], msg, 20);
            request.TryPeer(fakePeer.Object);
            
            Assert.False(request.HasTimedOut);
        }
    }
}
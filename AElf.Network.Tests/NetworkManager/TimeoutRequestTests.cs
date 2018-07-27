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
        public void FireRequest_ShouldEnqueueMessage()
        {
            Mock<IPeer> fakePeer = new Mock<IPeer>();
            fakePeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>()));
            
            Message msg = new Message();
            
            TimeoutRequest request = new TimeoutRequest(new byte[0], fakePeer.Object, msg, 1000);
            request.FireRequest();
            
            fakePeer.Verify(mock => mock.EnqueueOutgoing(It.IsAny<Message>()), Times.Once());
        }
        
        [Fact]
        public void FireWithNullPeer_ThrowsException()
        {
            TimeoutRequest request = new TimeoutRequest(new byte[0], null, null, 1000);
            var ex = Assert.Throws<InvalidOperationException>(() => request.FireRequest());
            Assert.Equal(ex.Message, "Peer cannot be null.");
        }
        
        [Fact]
        public void FireWithNullMessage_ThrowsException()
        {
            TimeoutRequest request = new TimeoutRequest(new byte[0], new Peer(1234), null, 1000);
            var ex = Assert.Throws<InvalidOperationException>(() => request.FireRequest());
            Assert.Equal(ex.Message, "RequestMessage cannot be null.");
        }

        [Fact]
        public void FireRequest_TimoutEvent_ShouldFire()
        {
            Mock<IPeer> fakePeer = new Mock<IPeer>();
            fakePeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>()));
            
            Message msg = new Message();
            
            TimeoutRequest request = new TimeoutRequest(new byte[0], fakePeer.Object, msg, 1);

            bool wasFired = false;
            request.RequestTimedOut += (sender, args) => wasFired = true;
            
            request.FireRequest();
            
            // Wait longer than the timout
            Thread.Sleep(50); 
            
            Assert.True(wasFired);
        }
        
        [Fact]
        public void FireRequest_WaitLongerThanTimout_ShouldNotFire()
        {
            Mock<IPeer> fakePeer = new Mock<IPeer>();
            fakePeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>()));
            
            Message msg = new Message();
            
            TimeoutRequest request = new TimeoutRequest(new byte[0], fakePeer.Object, msg, 50);

            bool wasFired = false;
            request.RequestTimedOut += (sender, args) => wasFired = true;
            
            request.FireRequest();
            
            // Wait for less time thant than the timout
            Thread.Sleep(10);
            
            Assert.False(wasFired);
        }
    }
}
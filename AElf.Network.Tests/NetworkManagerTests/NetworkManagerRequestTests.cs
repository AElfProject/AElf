using System;
using System.Collections.Generic;
using System.Threading;
using AElf.Network.Connection;
using AElf.Network.Peers;
using Moq;
using Xunit;

namespace AElf.Network.Tests.NetworkManagerTests
{
    public class NetworkManagerRequestTests
    {
        [Fact]
        public void QueueTransactionRequest_RetryOnTimeout()
        {
            Mock<IPeer> firstPeer = new Mock<IPeer>();
            firstPeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>()));
            
            Mock<IPeer> secondPeer = new Mock<IPeer>();
            secondPeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>()));
            
            NetworkManager manager = new NetworkManager(null, null, null);
            manager.AddPeer(firstPeer.Object);
            manager.AddPeer(secondPeer.Object);

            var txHash = new byte[] {0x01, 0x02};
            manager.QueueTransactionRequest(txHash, firstPeer.Object);
            
            Thread.Sleep(manager.RequestTimeout + 100);
            firstPeer.Verify(mock => mock.EnqueueOutgoing(It.IsAny<Message>()), Times.Once());
            
            Thread.Sleep(manager.RequestTimeout + 100);
            secondPeer.Verify(mock => mock.EnqueueOutgoing(It.IsAny<Message>()), Times.Once());
        }
        
        [Fact]
        public void QueueTransactionRequest_TryAllPeers_ShouldThrowEx()
        {
            Mock<IPeer> firstPeer = new Mock<IPeer>();
            firstPeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>()));
            
            NetworkManager manager = new NetworkManager(null, null, null);
            
            // Set tries to 1 : no retries.
            manager.RequestMaxRetry = 1;
            manager.AddPeer(firstPeer.Object);
            
            List<EventArgs> receivedEvents = new List<EventArgs>();

            manager.RequestFailed += (sender, args) => {
                receivedEvents.Add(args);
            };

            var txHash = new byte[] {0x01, 0x02};
            manager.QueueTransactionRequest(txHash, firstPeer.Object);
            
            // Wait for the request to timeout, the manager tests if it was the 
            // last try, if yes it throw the event.
            Thread.Sleep(manager.RequestTimeout + 100);
            firstPeer.Verify(mock => mock.EnqueueOutgoing(It.IsAny<Message>()), Times.Once());
            
            Assert.Equal(1, receivedEvents.Count);
            RequestFailedArgs reqFailArgs = Assert.IsType<RequestFailedArgs>(receivedEvents[0]);
            Assert.True(reqFailArgs.TriedPeers.Contains(firstPeer.Object));
        }
    }
}
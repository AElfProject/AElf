using System;
using System.Collections.Generic;
using System.Threading;
using AElf.Network.Connection;
using AElf.Network.Eventing;
using AElf.Network.Peers;
using AElf.Node.Protocol;
using AElf.Node.Protocol.Events;
using Moq;
using Xunit;

namespace AElf.Network.Tests.NetworkManagerTests
{
    public class NetworkManagerRequestTests
    {
//        [Fact]
//        public void QueueTransactionRequest_RetryOnTimeout()
//        {
//            Mock<IPeerManager> peerManager = new Mock<IPeerManager>();
//            
//            Mock<IPeer> firstPeer = new Mock<IPeer>();
//            firstPeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>()));
//            
//            Mock<IPeer> secondPeer = new Mock<IPeer>();
//            secondPeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>()));
//            
//            NetworkManager manager = new NetworkManager(null, peerManager.Object, null);
//            peerManager.Raise(m => m.PeerEvent += null, new PeerEventArgs(firstPeer.Object, PeerEventType.Added));
//            peerManager.Raise(m => m.PeerEvent += null, new PeerEventArgs(secondPeer.Object, PeerEventType.Added));
//
//            Message msg = new Message();
//            manager.QueueTransactionRequest(msg, firstPeer.Object);
//            
//            Thread.Sleep(manager.RequestTimeout + 100);
//            firstPeer.Verify(mock => mock.EnqueueOutgoing(It.IsAny<Message>()), Times.Once());
//            
//            Thread.Sleep(manager.RequestTimeout + 100);
//            secondPeer.Verify(mock => mock.EnqueueOutgoing(It.IsAny<Message>()), Times.Once());
//        }
//        
//        [Fact]
//        public void QueueTransactionRequest_TryAllPeers_ShouldThrowEx()
//        {
//            Mock<IPeerManager> peerManager = new Mock<IPeerManager>();
//            
//            Mock<IPeer> firstPeer = new Mock<IPeer>();
//            firstPeer.Setup(m => m.EnqueueOutgoing(It.IsAny<Message>()));
//            
//            NetworkManager manager = new NetworkManager(null, peerManager.Object, null);
//            
//            // Set tries to 1 : no retries.
//            manager.RequestMaxRetry = 1;
//            peerManager.Raise(m => m.PeerEvent += null, new PeerEventArgs(firstPeer.Object, PeerEventType.Added));
//            
//            List<EventArgs> receivedEvents = new List<EventArgs>();
//
//            manager.RequestFailed += (sender, args) => {
//                receivedEvents.Add(args);
//            };
//
//            Message msg = new Message();
//            manager.QueueTransactionRequest(msg, firstPeer.Object);
//            
//            // Wait for the request to timeout, the manager tests if it was the 
//            // last try, if yes it throw the event.
//            Thread.Sleep(manager.RequestTimeout + 100);
//            firstPeer.Verify(mock => mock.EnqueueOutgoing(It.IsAny<Message>()), Times.Once());
//            
//            Assert.Equal(1, receivedEvents.Count);
//            RequestFailedEventArgs reqFailEventArgs = Assert.IsType<RequestFailedEventArgs>(receivedEvents[0]);
//            Assert.True(reqFailEventArgs.TriedPeers.Contains(firstPeer.Object));
//        }
    }
}
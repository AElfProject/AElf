using System.Threading;
using AElf.Network.Connection;
using AElf.Network.Peers;
using Moq;
using Xunit;
using Xunit.Sdk;

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

            var txHash = new byte[2] {0x01, 0x02};
            manager.QueueTransactionRequest(txHash, firstPeer.Object);
            
            firstPeer.Verify(mock => mock.EnqueueOutgoing(It.IsAny<Message>()), Times.Once());
            Thread.Sleep(manager.RequestTimeout + 100);
            secondPeer.Verify(mock => mock.EnqueueOutgoing(It.IsAny<Message>()), Times.Once());
        }
    }
}
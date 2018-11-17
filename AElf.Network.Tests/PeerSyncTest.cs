using System;
using System.Net.Sockets;
using AElf.Cryptography.ECDSA;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.Network.Peers;
using Google.Protobuf;
using Moq;
using Xunit;

namespace AElf.Network.Tests
{
    public class PeerSyncTest
    {
        [Fact]
        public void SyncToHeight_WithValidTarget_TriggersSync()
        {
            // Mutiple things to verify: (1) state is correct, (2) a request is created for startHeight and (3) that 
            // the corresponding message is sent to the peer.
            
            int startHeight = 2;
            int targetHeight = 3;
            
            int peerPort = 1234;
            
            Mock<IMessageReader> reader = new Mock<IMessageReader>();
            Mock<IMessageWriter> messageWritter = new Mock<IMessageWriter>();

            ECKeyPair kp = new KeyPairGenerator().Generate();
            Peer p = new Peer(new TcpClient(), reader.Object, messageWritter.Object, peerPort, kp, 0);
            
            var (_, handshake) = NetworkTestHelpers.CreateKeyPairAndHandshake(1235);
            p.AuthentifyWith(handshake); // set "other peer as authentified"

            BlockRequest req = null;
            
            messageWritter
                .Setup(w => w.EnqueueMessage(It.Is<Message>(m => m.Type == (int)AElfProtocolMsgType.RequestBlock), It.IsAny<Action<Message>>()))
                .Callback<Message, Action<Message>>((m, a) =>
                {
                    if (m != null)
                    {
                        req = BlockRequest.Parser.ParseFrom(m.Payload);
                    }
                });
            
            p.SyncToHeight(startHeight, targetHeight);
            
            // Check state
            Assert.Equal(p.SyncTarget, targetHeight);
            Assert.Equal(p.CurrentlyRequestedHeight, startHeight);
            
            // Check block request 
            Assert.NotEmpty(p.BlockRequests);
            Assert.NotEmpty(p.BlockRequests);
            
            // Check request sent
            Assert.NotNull(req);
            Assert.Equal(req.Height, startHeight);
        }
        
        [Fact]
        public void SyncNextHistory_ReceiveTarget_EndsSync()
        {
            int startHeight = 2;
            int targetHeight = 2;
            
            int peerPort = 1234;
            
            Mock<IMessageReader> reader = new Mock<IMessageReader>();
            Mock<IMessageWriter> messageWritter = new Mock<IMessageWriter>();

            ECKeyPair kp = new KeyPairGenerator().Generate();
            Peer p = new Peer(new TcpClient(), reader.Object, messageWritter.Object, peerPort, kp, 0);
            
            var (_, handshake) = NetworkTestHelpers.CreateKeyPairAndHandshake(1235);
            p.AuthentifyWith(handshake); // set "other peer as authentified"
            
            p.SyncToHeight(startHeight, targetHeight);
            var movedToNext = p.SyncNextHistory();
            
            Assert.False(movedToNext);
            
            Assert.Equal(p.SyncTarget, 0);
            Assert.Equal(p.CurrentlyRequestedHeight, 0);
        }
        
        [Fact]
        public void SyncNextAnnouncement_NoStashAndNotSyncing_throws()
        {
            // Calling the method while not in sync and no requests is invalid.
            
            ECKeyPair kp = new KeyPairGenerator().Generate();
            Peer p = new Peer(new TcpClient(), null, null, 0, kp, 0);

            Assert.Throws<InvalidOperationException>(() => p.SyncNextAnnouncement());
        }

        [Fact]
        public void SyncNextAnnouncement_WithAnnouncement_TriggersSync()
        {
            Mock<IMessageReader> reader = new Mock<IMessageReader>();
            Mock<IMessageWriter> messageWritter = new Mock<IMessageWriter>();

            ECKeyPair kp = new KeyPairGenerator().Generate();
            Peer p = new Peer(new TcpClient(), reader.Object, messageWritter.Object, 1234, kp, 0);
            
            var (_, handshake) = NetworkTestHelpers.CreateKeyPairAndHandshake(1235);
            p.AuthentifyWith(handshake); // set "other peer as authentified"
            
            BlockRequest req = null;
            
            messageWritter
                .Setup(w => w.EnqueueMessage(It.Is<Message>(m => m.Type == (int)AElfProtocolMsgType.RequestBlock), It.IsAny<Action<Message>>()))
                .Callback<Message, Action<Message>>((m, a) =>
                {
                    if (m != null)
                    {
                        req = BlockRequest.Parser.ParseFrom(m.Payload);
                    }
                });

            var annoucement = new Announce { Height = 10, Id = ByteString.CopyFromUtf8("FakeHash")};
            p.StashAnnouncement(annoucement);
            
            // trigger sync
            var movedToNext = p.SyncNextAnnouncement();
            Assert.NotNull(req?.Id);
            
            // Should effectively request the block
            Assert.True(movedToNext);
            Assert.Equal(req.Height, 10);
            Assert.Equal(req.Id.ToStringUtf8(), "FakeHash");
            
            Assert.Equal(p.SyncedAnnouncement, annoucement);
            Assert.False(p.AnyStashed);
            
            Assert.Equal(p.SyncTarget, 0);
            Assert.Equal(p.CurrentlyRequestedHeight, 0);
        }
        
        [Fact]
        public void SyncNextAnnouncement_WithSyncingAndEmptyAnnoucements_EndsSync()
        {
            Mock<IMessageReader> reader = new Mock<IMessageReader>();
            Mock<IMessageWriter> messageWritter = new Mock<IMessageWriter>();

            ECKeyPair kp = new KeyPairGenerator().Generate();
            Peer p = new Peer(new TcpClient(), reader.Object, messageWritter.Object, 1234, kp, 0);
            
            var (_, handshake) = NetworkTestHelpers.CreateKeyPairAndHandshake(1235);
            p.AuthentifyWith(handshake); // set "other peer as authentified"

            var annoucement = new Announce { Height = 10, Id = ByteString.CopyFromUtf8("FakeHash")};
            p.StashAnnouncement(annoucement);
            
            // trigger sync
            p.SyncNextAnnouncement();
            
            // end sync
            var movedToNext = p.SyncNextAnnouncement();
            
            // Check that sync ended
            Assert.False(movedToNext);
            
            Assert.Null(p.SyncedAnnouncement);
            Assert.False(p.AnyStashed);
        }
    }
}
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.Network.Peers;
using Google.Protobuf;
using Moq;
using Xunit;

namespace AElf.Network.Tests
{
    public class PeerTests
    {
        [Fact]
        public void Peer_InitialState()
        {
            Peer p = new Peer(new TcpClient(), null, null, 1234, null, 0);
            Assert.False(p.IsAuthentified);
            Assert.False(p.IsDisposed);
        }

        [Fact]
        public void Start_Disposed_ThrowsException()
        {
            Peer p = new Peer(new TcpClient(), null, null, 1234, null, 0);
            p.Dispose();

            Assert.Throws<ObjectDisposedException>(() => p.Start());
        }
        
        [Fact]
        public void Start_Disposed_ThrowsInvalidOperationException()
        {
            int port = 1234;
            
            Peer p = new Peer(new TcpClient(), null, null, port, null, 0);
            
            var (_, handshake) = NetworkTestHelpers.CreateKeyPairAndHandshake(port);
            
            p.AuthentifyWith(handshake);

            var ex = Assert.Throws<InvalidOperationException>(() => p.Start());
            Assert.Equal("Cannot start an already authentified peer.", ex.Message);
        }
        
        [Fact]
        public void Start_ShouldSend_Auth()
        {
            int peerPort = 1234;
            
            Mock<IMessageReader> reader = new Mock<IMessageReader>();
            Mock<IMessageWriter> messageWritter = new Mock<IMessageWriter>();

            ECKeyPair kp = new KeyPairGenerator().Generate();
            Peer p = new Peer(new TcpClient(), reader.Object, messageWritter.Object, peerPort, kp, 0);

            Message authMessage = null;
            messageWritter.Setup(w => w.EnqueueMessage(It.IsAny<Message>(), It.IsAny<Action<Message>>())).Callback<Message, Action<Message>>((m, a) => authMessage = m);
            
            p.Start();
            
            Assert.NotNull(authMessage);
            Assert.Equal(0, authMessage.Type);

            Handshake handshake = Handshake.Parser.ParseFrom(authMessage.Payload);
            
            Assert.Equal(peerPort, handshake.NodeInfo.Port);
        }

        [Fact]
        public void Start_AuthentificationTimout_ShouldThrowEvent()
        {
            Mock<IMessageReader> reader = new Mock<IMessageReader>();
            Mock<IMessageWriter> messageWritter = new Mock<IMessageWriter>();
            
            ECKeyPair key = new KeyPairGenerator().Generate();
            Peer p = new Peer(new TcpClient(), reader.Object, messageWritter.Object, 1234, key, 0);
            p.AuthTimeout = 100;

            AuthFinishedArgs authFinishedArgs = null;
            
            p.AuthFinished += (sender, args) =>
            {
                authFinishedArgs = args as AuthFinishedArgs;
            };
            
            p.Start();
            
            Task.Delay(200).Wait();
            
            Assert.NotNull(authFinishedArgs);
            Assert.False(authFinishedArgs.IsAuthentified);
            Assert.True(authFinishedArgs.Reason == RejectReason.Auth_Timeout);
            Assert.False(p.IsAuthentified);
        }
        
        [Fact]
        public void Start_AuthentificationNoTimout_ShouldThrowEvent()
        {
            int localPort = 1234;
            int remotePort = 1235;
            
            Mock<IMessageReader> reader = new Mock<IMessageReader>();
            Mock<IMessageWriter> messageWritter = new Mock<IMessageWriter>();
            
            ECKeyPair key = new KeyPairGenerator().Generate();
            Peer p = new Peer(new TcpClient(), reader.Object, messageWritter.Object, localPort, key, 0);
            p.AuthTimeout = 100;
            
            AuthFinishedArgs authFinishedArgs = null;
            
            p.AuthFinished += (sender, args) => {
                authFinishedArgs = args as AuthFinishedArgs;
            };
            
            p.Start();
            
            var (_, handshake) = NetworkTestHelpers.CreateKeyPairAndHandshake(remotePort);
            p.AuthentifyWith(handshake);
            
            Task.Delay(200).Wait();
            
            Assert.Null(authFinishedArgs);
            Assert.True(p.IsAuthentified);
        }
    }
}
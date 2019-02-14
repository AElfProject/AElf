using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Account;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.Network.Peers;
using Google.Protobuf;
using Moq;
using Xunit;

namespace AElf.Network.Tests
{
    public class PeerTests : NetworkTestBase
    {
        private readonly IAccountService _accountService;
        
        public PeerTests()
        {
            _accountService = GetRequiredService<IAccountService>();
        }
        [Fact]
        public void Peer_InitialState()
        {
            Peer p = new Peer(new TcpClient(), null, null, 1234, 0, _accountService);
            Assert.False(p.IsAuthentified);
            Assert.False(p.IsDisposed);
        }

        [Fact]
        public void Start_Disposed_ThrowsException()
        {
            Peer p = new Peer(new TcpClient(), null, null, 1234, 0, _accountService);
            p.Dispose();

            Assert.Throws<ObjectDisposedException>(() => p.Start());
        }
        
        [Fact]
        public void Start_Disposed_ThrowsInvalidOperationException()
        {
            int port = 1234;
            
            Peer p = new Peer(new TcpClient(), null, null, port, 0, _accountService);
            
            var (_, handshake) = NetworkTestHelpers.CreateKeyPairAndHandshake(port);
            
            RejectReason reason = p.AuthentifyWith(handshake);
            Assert.Equal(RejectReason.None, reason);

            var ex = Assert.Throws<InvalidOperationException>(() => p.Start());
            Assert.Equal("Cannot start an already authentified peer.", ex.Message);
        }
        
        [Fact]
        public void Start_ShouldSend_Auth()
        {
            int peerPort = 1234;
            
            Mock<IMessageReader> reader = new Mock<IMessageReader>();
            Mock<IMessageWriter> messageWritter = new Mock<IMessageWriter>();

            Peer p = new Peer(new TcpClient(), reader.Object, messageWritter.Object, peerPort, 0, _accountService);

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
            
            Peer p = new Peer(new TcpClient(), reader.Object, messageWritter.Object, 1234, 0, _accountService);
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
            Assert.True(authFinishedArgs.Reason == RejectReason.AuthTimeout);
            Assert.False(p.IsAuthentified);
        }
        
        [Fact]
        public void Start_AuthentificationNoTimout_ShouldThrowEvent()
        {                
            int localPort = 1234;
            int remotePort = 1235;
            
            var (_, handshake) = NetworkTestHelpers.CreateKeyPairAndHandshake(remotePort);
            
            Mock<IMessageReader> reader = new Mock<IMessageReader>();
            Mock<IMessageWriter> messageWritter = new Mock<IMessageWriter>();
            
            Peer p = new Peer(new TcpClient(), reader.Object, messageWritter.Object, localPort, 0, _accountService);
            p.AuthTimeout = 10000;
            
            AuthFinishedArgs authFinishedArgs = null;
            
            p.AuthFinished += (sender, args) => {
                authFinishedArgs = args as AuthFinishedArgs;
            };

            // if (handshake.PublicKey == null || handshake.PublicKey.Length < 0)
            Assert.True(handshake.PublicKey != null);
            Assert.True(handshake.PublicKey.Length >= 0);

            p.Start();
            p.AuthentifyWith(handshake);
            
            Task.Delay(200).Wait();
            
            Assert.Null(authFinishedArgs);
            Assert.True(p.IsAuthentified);
        }
    }
}
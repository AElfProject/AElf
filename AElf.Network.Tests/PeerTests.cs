using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.Network.Peers;
using Moq;
using Xunit;

namespace AElf.Network.Tests
{
    public class PeerTests
    {
        [Fact]
        public void Peer_InitialState()
        {
            Peer p = new Peer(new TcpClient(), null, null, 1234, null);
            Assert.False(p.IsAuthentified);
            Assert.False(p.IsDisposed);
        }

        [Fact]
        public void Start_Disposed_ThrowsException()
        {
            Peer p = new Peer(new TcpClient(), null, null, 1234, null);
            p.Dispose();

            Assert.Throws<ObjectDisposedException>(() => p.Start());
        }
        
        [Fact]
        public void Start_Disposed_ThrowsInvalidOperationException()
        {
            Peer p = new Peer(new TcpClient(), null, null, 1234, null);
            
            p.AuthentifyWith(new NodeData { Port = 1234 });

            var ex = Assert.Throws<InvalidOperationException>(() => p.Start());
            Assert.Equal("Cannot start an already authentified peer.", ex.Message);
        }
        
        [Fact]
        public void Start_ShouldSend_Auth()
        {
            int peerPort = 1234;
            
            Mock<IMessageReader> reader = new Mock<IMessageReader>();
            Mock<IMessageWriter> messageWritter = new Mock<IMessageWriter>();
            
            Peer p = new Peer(new TcpClient(), reader.Object, messageWritter.Object, peerPort, ByteArrayHelpers.RandomFill(10));

            Message authMessage = null;
            messageWritter.Setup(w => w.EnqueueMessage(It.IsAny<Message>())).Callback<Message>(m => authMessage = m);
            
            p.Start();
            
            Assert.NotNull(authMessage);
            Assert.Equal(0, authMessage.Type);

            NodeData nd = NodeData.Parser.ParseFrom(authMessage.Payload);
            
            Assert.Equal(peerPort, nd.Port);
        }

        [Fact]
        public void Start_AuthentificationTimout_ShouldThrowEvent()
        {
            Mock<IMessageReader> reader = new Mock<IMessageReader>();
            Mock<IMessageWriter> messageWritter = new Mock<IMessageWriter>();
            
            Peer p = new Peer(new TcpClient(), reader.Object, messageWritter.Object, 1234, ByteArrayHelpers.RandomFill(1));
            p.AuthTimeout = 100;

            AuthFinishedArgs authFinishedArgs = null;
            
            p.AuthFinished += (sender, args) =>
            {
                authFinishedArgs = args as AuthFinishedArgs;
            };
            
            p.Start();
            Task.Delay(200).Wait();
            
            Assert.NotNull(authFinishedArgs);
            Assert.True(authFinishedArgs.HasTimedOut);
            Assert.False(p.IsAuthentified);
        }
        
        [Fact]
        public void Start_AuthentificationNoTimout_ShouldThrowEvent()
        {
            Mock<IMessageReader> reader = new Mock<IMessageReader>();
            Mock<IMessageWriter> messageWritter = new Mock<IMessageWriter>();
            
            Peer p = new Peer(new TcpClient(), reader.Object, messageWritter.Object, 1234, ByteArrayHelpers.RandomFill(1));
            p.AuthTimeout = 100;
            
            AuthFinishedArgs authFinishedArgs = null;
            
            p.AuthFinished += (sender, args) =>
            {
                authFinishedArgs = args as AuthFinishedArgs;
            };
            
            p.Start();
            p.AuthentifyWith(new NodeData { Port = 1235});
            Task.Delay(200).Wait();
            
            Assert.Null(authFinishedArgs);
            Assert.True(p.IsAuthentified);
        }
    }
}
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Account;
using AElf.Kernel.Services;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network;
using AElf.OS.Network.Grpc;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace AElf.OS.Network
{
    public class GrpcPeerPoolTests : OSCoreTestBase
    {
        private readonly IAccountService _accountService;
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IOptionsSnapshot<ChainOptions> _optionsMock;
            
        public GrpcPeerPoolTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _accountService = GetRequiredService<IAccountService>();
            
            var optionsMock = new Mock<IOptionsSnapshot<ChainOptions>>();
            optionsMock.Setup(m => m.Value).Returns(new ChainOptions { ChainId = ChainHelpers.GetRandomChainId() });
            _optionsMock = optionsMock.Object;
        }
        
        private GrpcPeerPool BuildPeerPool()
        {
            var optionsMock = new Mock<IOptionsSnapshot<NetworkOptions>>();
            optionsMock.Setup(m => m.Value).Returns(new NetworkOptions());
            
            var mockBlockChainService = new Mock<IFullBlockchainService>();
            mockBlockChainService.Setup(m => m.GetBestChainLastBlock())
                .Returns(Task.FromResult(new BlockHeader()));
            
            return new GrpcPeerPool( optionsMock.Object, _accountService, mockBlockChainService.Object);
        }
            
        [Fact]
        public void GetPeer_RemoteAddressOrPubKeyAlreadyPresent_ShouldReturnPeer()
        {
            var pool = BuildPeerPool();

            var remoteEndpoint = "127.0.0.1:6800";
            
            pool.AddPeer(new GrpcPeer(null, null, new HandshakeData { PublicKey = ByteString.CopyFrom(0x01, 0x02) }, remoteEndpoint, "6800"));
            
            Assert.NotNull(pool.FindPeerByAddress(remoteEndpoint));
            Assert.NotNull(pool.FindPeerByPublicKey(ByteString.CopyFrom(0x01, 0x02).ToByteArray()));
        }

        [Fact]
        public async Task AddPeerAsync_PeerAlreadyConnected_ShouldReturnFalse()
        {
            var pool = BuildPeerPool();
            
            var remoteEndpoint = "127.0.0.1:6800";
            
            pool.AddPeer(new GrpcPeer(null, null, new HandshakeData { PublicKey = ByteString.CopyFrom(0x01, 0x02) }, remoteEndpoint, "6800"));

            var added = await pool.AddPeerAsync(remoteEndpoint);
            
            Assert.False(added);
        }
    }
}
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Grpc;
using Microsoft.Extensions.Options;
using Xunit;

namespace AElf.OS.Network
{
    public class GrpcPeerPoolTests : OSCoreTestBase
    {
        private const string TestIp = "127.0.0.1:6800";
        private readonly string _testPubKey;
        
        private readonly IOptionsSnapshot<NetworkOptions> _networkOptions;
        
        private readonly IAccountService _accountService;
        private readonly IBlockchainService _blockchainService;

        public GrpcPeerPoolTests()
        {
            _accountService = GetRequiredService<IAccountService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _networkOptions = GetRequiredService<IOptionsSnapshot<NetworkOptions>>();

            var keyPair = CryptoHelpers.GenerateKeyPair();
            _testPubKey = keyPair.PublicKey.ToHex();
        }
        
        private GrpcPeerPool BuildPeerPool()
        {
            return new GrpcPeerPool(_networkOptions, _accountService, _blockchainService);
        }
            
        [Fact]
        public void GetPeer_RemoteAddressOrPubKeyAlreadyPresent_ShouldReturnPeer()
        {
            var pool = BuildPeerPool();
            pool.AddPeer(new GrpcPeer(null, null, _testPubKey, TestIp));
            
            Assert.NotNull(pool.FindPeerByAddress(TestIp));
            Assert.NotNull(pool.FindPeerByPublicKey(_testPubKey));
        }

        [Fact]
        public async Task AddPeerAsync_PeerAlreadyConnected_ShouldReturnFalse()
        {
            var pool = BuildPeerPool();
            pool.AddPeer(new GrpcPeer(null, null, _testPubKey, TestIp));

            var added = await pool.AddPeerAsync(TestIp);
            
            Assert.False(added);
        }
    }
}
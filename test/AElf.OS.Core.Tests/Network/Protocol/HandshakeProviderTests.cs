using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Protocol
{
    public class HandshakeProviderTests : HandshakeTestBase
    {
        private readonly IHandshakeProvider _handshakeProvider;
        private readonly TestPeerKeyProvider _peerKeyProvider;
        private readonly IBlockchainService _blockchainService;

        public HandshakeProviderTests()
        {
            _handshakeProvider = GetRequiredService<IHandshakeProvider>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _peerKeyProvider = GetRequiredService<TestPeerKeyProvider>();
        }
        
        public Handshake CreateHandshake(ECKeyPair initiatorPeer, int chainId = NetworkTestConstants.DefaultChainId)
        {
            var data = new HandshakeData
            {
                ChainId = _blockchainService.GetChainId(),
                Version = KernelConstants.ProtocolVersion,
                Pubkey = ByteString.CopyFrom(initiatorPeer.PublicKey),
                Time = TimestampHelper.GetUtcNow()
            };
            
            var signature = CryptoHelper.SignWithPrivateKey(initiatorPeer.PrivateKey, Hash.FromMessage(data).ToByteArray());
            
            return new Handshake { HandshakeData = data, Signature = ByteString.CopyFrom(signature) };
        }

        [Fact]
        public async Task ValidateHandshake_Test()
        {
            var remoteKeyPair = _peerKeyProvider.AuthorizedKey;

            var handshake = CreateHandshake(remoteKeyPair);
            var validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBe(HandshakeValidationResult.Ok);

            handshake = CreateHandshake(remoteKeyPair);
            var unauthorizedKeyPair = CryptoHelper.GenerateKeyPair();
            handshake.HandshakeData.Pubkey = ByteString.CopyFrom(unauthorizedKeyPair.PublicKey);
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBe(HandshakeValidationResult.Unauthorized);

            handshake = CreateHandshake(remoteKeyPair);
            handshake.HandshakeData.ChainId = 1234;
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBe(HandshakeValidationResult.InvalidChainId);

            handshake = CreateHandshake(remoteKeyPair);
            handshake.HandshakeData.Version = 0;
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBe(HandshakeValidationResult.InvalidVersion);

            handshake = CreateHandshake(remoteKeyPair);
            handshake.HandshakeData.Time =
                TimestampHelper.GetUtcNow().AddMilliseconds(-(NetworkConstants.HandshakeTimeout + 100));
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBe(HandshakeValidationResult.HandshakeTimeout);

            handshake = CreateHandshake(remoteKeyPair);
            var maliciousPeer = CryptoHelper.GenerateKeyPair();
            var signature = CryptoHelper.SignWithPrivateKey(maliciousPeer.PrivateKey, Hash
                .FromMessage(handshake.HandshakeData)
                .ToByteArray());
            handshake.Signature = ByteString.CopyFrom(signature);
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBe(HandshakeValidationResult.InvalidSignature);

            var localHandshake = await _handshakeProvider.GetHandshakeAsync();
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(localHandshake);
            validationResult.ShouldBe(HandshakeValidationResult.SelfConnection);
        }
    }
}
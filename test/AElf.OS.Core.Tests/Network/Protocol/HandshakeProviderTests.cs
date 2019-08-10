using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
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

        public HandshakeProviderTests()
        {
            _handshakeProvider = GetRequiredService<IHandshakeProvider>();
        }

        [Fact]
        public async Task ValidateHandshake_Test()
        {
            var handshake = await _handshakeProvider.GetHandshakeAsync();
            var validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBe(HandshakeValidationResult.Ok);

            handshake = await _handshakeProvider.GetHandshakeAsync();
            var fakeKeyPair = CryptoHelper.GenerateKeyPair();
            handshake.HandshakeData.Pubkey = ByteString.CopyFrom(fakeKeyPair.PublicKey);
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBe(HandshakeValidationResult.Unauthorized);

            handshake = await _handshakeProvider.GetHandshakeAsync();
            handshake.HandshakeData.ChainId = 1234;
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBe(HandshakeValidationResult.InvalidChainId);

            handshake = await _handshakeProvider.GetHandshakeAsync();
            handshake.HandshakeData.Version = 0;
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBe(HandshakeValidationResult.InvalidVersion);

            handshake = await _handshakeProvider.GetHandshakeAsync();
            handshake.HandshakeData.Time =
                TimestampHelper.GetUtcNow().AddMilliseconds(-(NetworkConstants.HandshakeTimeout + 100));
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBe(HandshakeValidationResult.HandshakeTimeout);

            handshake = await _handshakeProvider.GetHandshakeAsync();
            var signature = CryptoHelper.SignWithPrivateKey(fakeKeyPair.PrivateKey, Hash
                .FromMessage(handshake.HandshakeData)
                .ToByteArray());
            handshake.Signature = ByteString.CopyFrom(signature);
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBe(HandshakeValidationResult.InvalidSignature);
        }
    }
}
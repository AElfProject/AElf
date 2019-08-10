using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Protocol
{
    public class HandshakeProviderTests : NetworkTestBase
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
            handshake.HandshakeData.Time =
                TimestampHelper.GetUtcNow().AddMilliseconds(-(NetworkConstants.HandshakeTimeout + 100));
            var validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldNotBe(HandshakeValidationResult.HandshakeTimeout);
            
            handshake = await _handshakeProvider.GetHandshakeAsync();
            var fakeKeyPair = CryptoHelper.GenerateKeyPair();
            handshake.HandshakeData.Pubkey = ByteString.CopyFrom(fakeKeyPair.PublicKey);
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldNotBe(HandshakeValidationResult.InvalidSignature);
            
            handshake = await _handshakeProvider.GetHandshakeAsync();
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldNotBe(HandshakeValidationResult.Ok);
        }
    }
}
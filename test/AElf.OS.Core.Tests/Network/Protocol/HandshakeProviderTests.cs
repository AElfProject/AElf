using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.OS.Network.Infrastructure;
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
            Handshake handshake = null;

            var validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBeFalse();

            handshake = new Handshake();
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBeFalse();

            handshake = await _handshakeProvider.GetHandshakeAsync();
            handshake.HandshakeData.Time =
                TimestampHelper.GetUtcNow().AddMilliseconds(-(NetworkConstants.HandshakeTimeout + 100));
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBeFalse();
            
            handshake = await _handshakeProvider.GetHandshakeAsync();
            var fakeKeyPair = CryptoHelper.GenerateKeyPair();
            handshake.HandshakeData.Pubkey = ByteString.CopyFrom(fakeKeyPair.PublicKey);
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBeFalse();
            
            handshake = await _handshakeProvider.GetHandshakeAsync();
            validationResult = await _handshakeProvider.ValidateHandshakeAsync(handshake);
            validationResult.ShouldBeTrue();
        }
    }
}
using AElf.Cryptography.EdDSA;
using Shouldly;
using Xunit;

namespace AElf.Cryptography.Tests;

public class Bn254HelperTest
{
    [Fact]
    public void Ed25519Verify_Test()
    {
        var publicKey = "d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a";
        var message = "";
        var signature =
            "e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e065224901555fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b";

        var publicKeyBytes = ByteArrayHelper.HexStringToByteArray(publicKey);
        var messageBytes = ByteArrayHelper.HexStringToByteArray(message);
        var signatureBytes = ByteArrayHelper.HexStringToByteArray(signature);

        var ed25519VerifyResult = EdDsaHelper.Ed25519Verify(signatureBytes, messageBytes, publicKeyBytes);

        ed25519VerifyResult.ShouldBe(true);
    }
}
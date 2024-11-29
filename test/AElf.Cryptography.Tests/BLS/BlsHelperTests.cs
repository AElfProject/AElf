using System.Text;
using AElf.Cryptography.Bls;
using Shouldly;
using Xunit;

namespace AElf.Cryptography.Tests.BLS;

public class BlsHelperTests
{
    private static readonly string PrivateKey = "ca4ec1afc1c790dcd063cc7c52710e6029ca499c582db042154ab8f55fae3e29";
    private static readonly string Message = "Hello world";

    [Fact]
    public void VerifySignatureTest()
    {
        var privateKey = ByteArrayHelper.HexStringToByteArray(PrivateKey);
        var blsPubkey = BlsHelper.GetBlsPubkey(privateKey);
        var message = Encoding.UTF8.GetBytes(Message);
        var signature = BlsHelper.SignWithSecretKey(privateKey, message);
        BlsHelper.VerifySignature(signature, message, blsPubkey).ShouldBeTrue();
    }

    [Fact]
    public void AggregateTest()
    {
        const string privateKey1 = "ca4ec1afc1c790dcd063cc7c52710e6029ca499c582db042154ab8f55fae3e29";
        const string privateKey2 = "01c1b4bebc4a4e3175c470cadbe79e6d7c6836b6d4c24637553bb6376928e13a";
        const string privateKey3 = "50e0192c38ce532ea6c97b7826ab1dc2c41d9a068377b4a9025661129279841c";

        var privateKeyBytes1 = ByteArrayHelper.HexStringToByteArray(privateKey1);
        var privateKeyBytes2 = ByteArrayHelper.HexStringToByteArray(privateKey2);
        var privateKeyBytes3 = ByteArrayHelper.HexStringToByteArray(privateKey3);

        var message = Encoding.UTF8.GetBytes(Message);

        var signature1 = BlsHelper.SignWithSecretKey(privateKeyBytes1, message);
        var signature2 = BlsHelper.SignWithSecretKey(privateKeyBytes2, message);
        var signature3 = BlsHelper.SignWithSecretKey(privateKeyBytes3, message);

        var aggregatedSignature = BlsHelper.AggregateSignatures(new[] { signature1, signature2, signature3 });
        var aggregatedPubkey = BlsHelper.AggregatePubkeys(new[]
        {
            BlsHelper.GetBlsPubkey(privateKeyBytes1),
            BlsHelper.GetBlsPubkey(privateKeyBytes2),
            BlsHelper.GetBlsPubkey(privateKeyBytes3),
        });
        BlsHelper.VerifySignature(aggregatedSignature, message, aggregatedPubkey).ShouldBeTrue();
    }
}
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Xunit;

namespace AElf.Cryptography.Tests.ECDSA;

public class BlockSignatureTest
{
    [Fact]
    public void SignAndVerifyTransaction_Test()
    {
        var keyPair1 = CryptoHelper.GenerateKeyPair();
        var tx = new Transaction();
        tx.From = Address.FromPublicKey(keyPair1.PublicKey);

        var keyPair2 = CryptoHelper.GenerateKeyPair();
        tx.To = Address.FromPublicKey(keyPair2.PublicKey);
        tx.Params = ByteString.CopyFrom();
        tx.MethodName = "TestMethod";
        tx.Params = ByteString.Empty;
        tx.RefBlockNumber = 1;
        tx.RefBlockPrefix = ByteString.Empty;

        // Serialize and hash the transaction
        var hash = tx.GetHash();

        // Sign the hash
        var signature = CryptoHelper.SignWithPrivateKey(keyPair1.PrivateKey, hash.ToByteArray());
        tx.Signature = ByteString.CopyFrom(signature);
        Assert.True(tx.VerifySignature());
    }
}
using System;
using System.Text;
using AElf.Cryptography.Exceptions;
using AElf.Types;
using Org.BouncyCastle.Utilities.Encoders;
using Shouldly;
using Virgil.Crypto;
using Xunit;

namespace AElf.Cryptography.Tests;

public class CryptoHelperTests
{
    [Fact]
    public void Generate_Key_Test()
    {
        var keyPair = CryptoHelper.GenerateKeyPair();
        keyPair.ShouldNotBeNull();
        keyPair.PrivateKey.Length.ShouldBe(32);
        keyPair.PublicKey.Length.ShouldBe(65);

        //invalid key length
        var bytes = new byte[30];
        new Random().NextBytes(bytes);
        Assert.Throws<InvalidPrivateKeyException>(() => CryptoHelper.FromPrivateKey(bytes));
    }

    [Fact]
    public void Generate_KeyPair_Not_Same_Test()
    {
        var keyPair1 = CryptoHelper.GenerateKeyPair();
        var keyPair2 = CryptoHelper.GenerateKeyPair();
        keyPair1.ShouldNotBe(keyPair2);
    }

    [Fact]
    public void Recover_Public_Key_Test()
    {
        var keyPair = CryptoHelper.GenerateKeyPair();

        var messageBytes1 = Encoding.UTF8.GetBytes("Hello world.");
        var messageHash1 = messageBytes1.ComputeHash();

        var messageBytes2 = Encoding.UTF8.GetBytes("Hello aelf.");
        var messageHash2 = messageBytes2.ComputeHash();

        var signature1 = CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, messageHash1);

        var recoverResult1 = CryptoHelper.RecoverPublicKey(signature1, messageHash1, out var publicKey1);

        Assert.True(recoverResult1);
        Assert.True(publicKey1.BytesEqual(keyPair.PublicKey));

        var recoverResult2 = CryptoHelper.RecoverPublicKey(signature1, messageHash2, out var publicKey2);

        Assert.True(recoverResult2);
        Assert.False(publicKey2.BytesEqual(keyPair.PublicKey));

        var invalidSignature = ByteArrayHelper.HexStringToByteArray(
            "1c9469cbd4b9f722056d3eafd9823b14be9d2759192a7980aafba9d767576834ce25cb570e63dede117ff5c831e33ac47d0450b6b4cea0d04d66a435f2275ef3ec");
        var recoverResult3 = CryptoHelper.RecoverPublicKey(invalidSignature, messageHash2, out var publicKey3);
        Assert.False(recoverResult3);

        var invalidSignature2 = new byte[10];
        var recoverResult4 = CryptoHelper.RecoverPublicKey(invalidSignature2, messageHash2, out var publicKey4);
        Assert.False(recoverResult4);
    }

    [Fact]
    public void VerifySignature_BadSignature()
    {
        var keyPair = CryptoHelper.GenerateKeyPair();
        var messageBytes = Encoding.UTF8.GetBytes("Hello world.");
        var messageHash = messageBytes.ComputeHash();
        var signature = new byte[65];

        var verifyResult = CryptoHelper.VerifySignature(signature, messageHash, keyPair.PublicKey);
        verifyResult.ShouldBeFalse();
    }

    [Fact]
    public void FromPrivateKey_BadPrivateKey_ShouldThrowException()
    {
        Should.Throw<InvalidPrivateKeyException>(() => CryptoHelper.FromPrivateKey(new byte[32]));
        Should.Throw<InvalidPrivateKeyException>(() => CryptoHelper.FromPrivateKey(null));
        Should.Throw<InvalidPrivateKeyException>(() => CryptoHelper.FromPrivateKey(new byte[0]));
    }

    [Fact]
    public void Decrypt_Message_Test()
    {
        var alice = CryptoHelper.GenerateKeyPair();
        var bob = CryptoHelper.GenerateKeyPair();
        var sam = CryptoHelper.GenerateKeyPair();

        // Alice want to transmit plain text "aelf" to Bob.

        var plainText = Encoding.UTF8.GetBytes(HashHelper.ComputeFrom("hash").ToHex());
        var cipherText = CryptoHelper.EncryptMessage(alice.PrivateKey, bob.PublicKey, plainText);

        // Bob decrypt the message.
        var decrypt = CryptoHelper.DecryptMessage(alice.PublicKey, bob.PrivateKey, cipherText);
        Assert.True(decrypt.BytesEqual(plainText));

        // Sam can't decrypt this message.
        var func = new Func<byte[]>(() => CryptoHelper.DecryptMessage(alice.PublicKey, sam.PrivateKey,
            cipherText));
        Assert.Throws<VirgilCryptoException>(func);
    }

    [Fact]
    public void Ecdh_Test()
    {
        var alice = CryptoHelper.GenerateKeyPair();
        var bob = CryptoHelper.GenerateKeyPair();

        var ecdhKey1 = CryptoHelper.Ecdh(alice.PrivateKey, bob.PublicKey);
        var ecdhKey2 = CryptoHelper.Ecdh(bob.PrivateKey, alice.PublicKey);

        Assert.Equal(ecdhKey1.ToHex(), ecdhKey2.ToHex());
    }

    [Fact]
    public void Ecdh_BadArgument_ShouldThrowException()
    {
        Assert.Throws<PublicKeyOperationException>(() => CryptoHelper.Ecdh(new byte[32], new byte[33]));
    }

    [Fact]
    public void ExceptionTest()
    {
        var message = "message";
        Should.Throw<EcdhOperationException>(() => throw new EcdhOperationException(message));
        Should.Throw<SignatureOperationException>(() => throw new SignatureOperationException(message));
        Should.Throw<InvalidKeyPairException>(() => throw new InvalidKeyPairException(message, new Exception()));
    }

    [Fact]
    public void VrfTest()
    {
        var key = CryptoHelper.FromPrivateKey(
            ByteArrayHelper.HexStringToByteArray("5945c176c4269dc2aa7daf7078bc63b952832e880da66e5f2237cdf79bc59c5f"));
        var alpha = "5cf8151010716e40e5349ad02821da605df22e9ac95450c7e35f04c720fd4db5";
        var alphaBytes = Hash.LoadFromHex(alpha).ToByteArray();
        var pi = CryptoHelper.ECVrfProve(key, alphaBytes);
        var beta = CryptoHelper.ECVrfVerify(key.PublicKey, alphaBytes, pi);
        beta.ToHex().ShouldBe("43765915e86c205f0c28b4d22e157b8474add7faf890a3aa2a88df756651523f");
    }
}
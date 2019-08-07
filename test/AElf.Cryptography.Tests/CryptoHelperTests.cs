using System;
using System.Security.Cryptography;
using System.Text;
using AElf.Types;
using Xunit;
using Shouldly;
using Virgil.Crypto;

namespace AElf.Cryptography.Tests
{
    public class CryptoHelperTests
    {
        [Fact]
        public void Test_Generate_Key()
        {
            CryptoHelper.GenerateKeyPair();
        }

        [Fact]
        public void Test_Generate_KeyPair_Not_Same()
        {
            var keyPair1 = CryptoHelper.GenerateKeyPair();
            var keyPair2 = CryptoHelper.GenerateKeyPair();
            keyPair1.ShouldNotBe(keyPair2);
        }

        [Fact]
        public void Test_Recover_Public_key()
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
        public void FromPrivateKey_BadPrivateKey_ShouldThrowArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CryptoHelper.FromPrivateKey(new byte[32]));
        }
        
        [Fact]
        public void Test_Decrypt_Message()
        {
            var alice = CryptoHelper.GenerateKeyPair();
            var bob = CryptoHelper.GenerateKeyPair();
            var sam = CryptoHelper.GenerateKeyPair();

            // Alice want to transmit plain text "aelf" to Bob.

            var plainText = Encoding.UTF8.GetBytes(Hash.FromString("hash").ToHex());
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
        public void Test_RandomByteArrayGenerate()
        {
            var byteArray1 = CryptoHelper.RandomFill(30);
            byteArray1.Length.ShouldBe(30);
        }
        
        [Fact]
        public void Ecdh_BadArgument_ShouldThrowArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CryptoHelper.Ecdh(new byte[32], new byte[33]));
        }
    }
}
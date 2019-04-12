using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using AElf.Common;
using AElf.Database.RedisProtocol;
using Secp256k1Net;
using Xunit;
using Shouldly;
using Virgil.Crypto;

namespace AElf.Cryptography.Tests
{
    public class CryptoHelpersTests
    {
        [Fact]
        public void Test_Generate_Key()
        {
            CryptoHelpers.GenerateKeyPair();
        }

        [Fact]
        public void Test_Generate_KeyPair_Not_Same()
        {
            var keyPair1 = CryptoHelpers.GenerateKeyPair();
            var keyPair2 = CryptoHelpers.GenerateKeyPair();
            keyPair1.ShouldNotBe(keyPair2);
        }

        [Fact]
        public void Test_Recover_Public_key()
        {
            var keyPair = CryptoHelpers.GenerateKeyPair();

            var messageBytes1 = Encoding.UTF8.GetBytes("Hello world.");
            var messageHash1 = SHA256.Create().ComputeHash(messageBytes1);

            var messageBytes2 = Encoding.UTF8.GetBytes("Hello aelf.");
            var messageHash2 = SHA256.Create().ComputeHash(messageBytes2);

            var signature1 = CryptoHelpers.SignWithPrivateKey(keyPair.PrivateKey, messageHash1);

            var recoverResult1 = CryptoHelpers.RecoverPublicKey(signature1, messageHash1, out var publicKey1);

            Assert.True(recoverResult1);
            Assert.True(publicKey1.BytesEqual(keyPair.PublicKey));

            var recoverResult2 = CryptoHelpers.RecoverPublicKey(signature1, messageHash2, out var publicKey2);

            Assert.True(recoverResult2);
            Assert.False(publicKey2.BytesEqual(keyPair.PublicKey));
        }


        [Fact]
        public void Test_Decrypt_Message()
        {
            var alice = CryptoHelpers.GenerateKeyPair();
            var bob = CryptoHelpers.GenerateKeyPair();
            var sam = CryptoHelpers.GenerateKeyPair();

            // Alice want to transmit plain text "aelf" to Bob.

            var plainText = Encoding.UTF8.GetBytes(Hash.Generate().ToHex());
            var cipherText = CryptoHelpers.EncryptMessage(alice.PrivateKey, bob.PublicKey, plainText);

            // Bob decrypt the message.
            var decrypt = CryptoHelpers.DecryptMessage(alice.PublicKey, bob.PrivateKey, cipherText);
            Assert.True(decrypt.BytesEqual(plainText));

            // Sam can't decrypt this message.
            var func = new Func<byte[]>(() => CryptoHelpers.DecryptMessage(alice.PublicKey, sam.PrivateKey,
                cipherText));
            Assert.Throws<VirgilCryptoException>(func);
        }

        [Fact]
        public void Ecdh_Test()
        {
            var alice = CryptoHelpers.GenerateKeyPair();
            var bob = CryptoHelpers.GenerateKeyPair();

            var ecdhKey1 = CryptoHelpers.Ecdh(alice.PrivateKey, bob.PublicKey);
            var ecdhKey2 = CryptoHelpers.Ecdh(bob.PrivateKey, alice.PublicKey);

            Assert.Equal(ecdhKey1.ToHex(), ecdhKey2.ToHex());
        }

        [Fact]
        public void Test_RandomByteArrayGenerate()
        {
            var byteArray1 = CryptoHelpers.RandomFill(30);
            byteArray1.Length.ShouldBe(30);
        }
    }
}
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using AElf.Common;
using Xunit;
using Shouldly;

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
        public void Test_RandomByteArrayGenerate()
        {
            var byteArray1 = CryptoHelpers.RandomFill(30);
            byteArray1.Length.ShouldBe(30);
        }
    }
}
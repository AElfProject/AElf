using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types;
using Xunit;
using Google.Protobuf;

namespace AElf.Cryptography.Tests.ECDSA
{
    public class BlockSignatureTest
    {
        [Fact]
        public void SignAndVerifyTransaction()
        {
            ECKeyPair keyPair1 = CryptoHelper.GenerateKeyPair();
            Transaction tx = new Transaction();
            tx.From = Address.FromPublicKey(keyPair1.PublicKey);
            
            ECKeyPair keyPair2 = CryptoHelper.GenerateKeyPair();
            tx.To = Address.FromPublicKey(keyPair2.PublicKey);
            tx.Params = ByteString.CopyFrom(new byte[0]);
            tx.MethodName = "TestMethod";
            tx.Params = ByteString.Empty;
            tx.RefBlockNumber = 1;
            tx.RefBlockPrefix = ByteString.Empty;

            // Serialize and hash the transaction
            Hash hash = tx.GetHash();

            // Sign the hash
            var signature = CryptoHelper.SignWithPrivateKey(keyPair1.PrivateKey, hash.ToByteArray());
            tx.Signature = ByteString.CopyFrom(signature);
            Assert.True(tx.VerifySignature());
        }
    }
}
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Xunit;
using Google.Protobuf;

namespace AElf.Cryptography.Tests.ECDSA
{
    public class BlockSignatureTest
    {
        // The length of an AElf address
        // todo : modify this constant when we reach an agreement on the length
        private const int ADR_LENGTH = 42;
        
        [Fact]
        public void SignAndVerifyTransaction()
        {
            ECKeyPair keyPair = CryptoHelpers.GenerateKeyPair();
            Transaction tx = new Transaction();
            tx.From = Address.FromPublicKey(keyPair.PublicKey);
            tx.To = Address.Generate();
            tx.Params = ByteString.CopyFrom(new byte[0]);
            tx.MethodName = "TestMethod";
            tx.Params = ByteString.Empty;
            tx.RefBlockNumber = 1;
            tx.RefBlockPrefix = ByteString.Empty;

            // Serialize and hash the transaction
            Hash hash = tx.GetHash();

            // Sign the hash
            var signature = CryptoHelpers.SignWithPrivateKey(keyPair.PrivateKey, hash.DumpByteArray());
            tx.Signature = ByteString.CopyFrom(signature);
            Assert.True(tx.VerifySignature());
        }
    }
}
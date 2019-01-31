using System;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Xunit;
using AElf.Common;
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
            //Console.WriteLine("From: {0}", tx.From);
            //Console.WriteLine("Public key: {0}", keyPair.PublicKey.ToHex());
            
            // Serialize and hash the transaction
            Hash hash = tx.GetHash();

            // Sign the hash
            var signature = CryptoHelpers.SignWithPrivateKey(keyPair.PrivateKey, hash.DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));
            Assert.True(tx.VerifySignature());
        }
    }
}
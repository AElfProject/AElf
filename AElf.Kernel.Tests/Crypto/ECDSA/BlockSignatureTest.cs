using AElf.Kernel.Crypto;
using AElf.Kernel.Crypto.ECDSA;
using Xunit;

namespace AElf.Kernel.Tests.Crypto.ECDSA
{
    public class BlockSignatureTest
    {
        // The length of an AElf address
        // todo : modify this constant when we reach an agreement on the length
        private const int ADR_LENGTH = 42;
        
        [Fact]
        public void SignAndVerifyTransaction()
        {
            // Generate the key pair 
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
        
            Transaction tx = new Transaction();
            tx.From = new Hash(CryptoHelpers.RandomFill(ADR_LENGTH));
            tx.To = new Hash(CryptoHelpers.RandomFill(ADR_LENGTH));
            
            Block block = new Block();
            block.AddTransaction(tx.GetHash());
            
            // Serialize and hash the transaction
            Hash hash = tx.GetHash();
        
            // Sign the hash
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(keyPair, hash.GetHashBytes());
        
            ECVerifier verifier = new ECVerifier(keyPair);
        
            Assert.True(verifier.Verify(signature, hash.GetHashBytes()));
        }
    }
}
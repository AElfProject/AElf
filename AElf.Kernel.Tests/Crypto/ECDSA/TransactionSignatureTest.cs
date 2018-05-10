using AElf.Kernel.Crypto;
using AElf.Kernel.Crypto.ECDSA;
using Google.Protobuf;
using Xunit;

namespace AElf.Kernel.Tests.Crypto.ECDSA
{
    public class TransactionSignatureTest
    {
        // The length of an AElf address
        // todo : modify this constant when we reach an agreement on the length
        private const int ADR_LENGTH = 42;
            
        [Fact]
        public void SignAndVerifyTransaction()
        {
            byte[] fromAdress = CryptoHelpers.RandomFill(ADR_LENGTH);
            byte[] toAdress = CryptoHelpers.RandomFill(ADR_LENGTH);
            
            // Generate the key pair 
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            
            Transaction tx = new Transaction();
            tx.From = new Hash(fromAdress);
            tx.To = new Hash(toAdress);
            tx.P =  ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            
            // Serialize and hash the transaction
            Hash hash = tx.GetHash();
            
            // Sign the hash
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(keyPair, hash.GetHashBytes());
            
            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);
            
            // Serialize as for sending over the network
            byte[] serializedTx = tx.Serialize();
            
            /**** broadcast/receive *****/
            
            Transaction dTx = Transaction.Parser.ParseFrom(serializedTx);
            
            // Serialize and hash the transaction
            Hash dHash = dTx.GetHash();
            
            byte[] uncompressedPrivKey = tx.P.ToByteArray();

            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            ECVerifier verifier = new ECVerifier(recipientKeyPair);
            
            Assert.True(verifier.Verify(dTx.GetSignature(), dHash.GetHashBytes()));
        }
    }
}
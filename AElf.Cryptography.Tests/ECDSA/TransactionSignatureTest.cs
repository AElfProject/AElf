using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Types;
using Google.Protobuf;
using Xunit;
using AElf.Common;

namespace AElf.Cryptography.Tests.ECDSA
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


            ;
            
            Transaction tx = new Transaction();
            tx.From = Address.FromBytes(fromAdress);
            tx.To = Address.FromBytes(toAdress);
            tx.P =  ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            
            // Serialize and hash the transaction
            Hash hash = tx.GetHash();
            
            // Sign the hash
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(keyPair, hash.Dump());
            
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
            
            Assert.True(verifier.Verify(dTx.GetSignature(), dHash.Dump()));
        }
    }
}
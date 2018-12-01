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

            Transaction tx = new Transaction();
            tx.From = Address.FromRawBytes(fromAdress);
            tx.To = Address.FromRawBytes(toAdress);
            var sig = new Sig
            {
                P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded())
            };
            tx.Sigs.Add(sig);
            
            // Serialize and hash the transaction
            Hash hash = tx.GetHash();
            
            // Sign the hash
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(keyPair, hash.DumpByteArray());
            
            // Update the signature
            sig.R = ByteString.CopyFrom(signature.R);
            sig.S = ByteString.CopyFrom(signature.S);
            
            // Serialize as for sending over the network
            byte[] serializedTx = tx.Serialize();
            
            /**** broadcast/receive *****/
            
            Transaction dTx = Transaction.Parser.ParseFrom(serializedTx);
            
            // Serialize and hash the transaction
            Hash dHash = dTx.GetHash();
            
            byte[] uncompressedPrivKey = sig.P.ToByteArray();

            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            ECVerifier verifier = new ECVerifier(recipientKeyPair);

            foreach (var s in tx.Sigs)
            {
                Assert.True(verifier.Verify(new ECSignature(s.R.ToByteArray(), s.S.ToByteArray()), dHash.DumpByteArray()));
            }
            
        }
    }
}
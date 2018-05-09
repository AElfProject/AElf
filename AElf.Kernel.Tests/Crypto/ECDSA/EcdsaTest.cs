using System.Linq;
using AElf.Kernel.Crypto.ECDSA;
using Org.BouncyCastle.Math;
using Xunit;

namespace AElf.Kernel.Tests.Crypto.ECDSA
{
    public class EcdsaTest
    {
        [Fact]
        public void VerifySignature_WrongData_ReturnsFalse()
        {
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            
            byte[] message = new BigInteger("968236873715988614170569073515315707566766479517").ToByteArray();
            
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(keyPair, message);
            
            // Change the message 
            message = new BigInteger("9682368737159881417056907351531570756676647951").ToByteArray();
            
            ECVerifier verifier = new ECVerifier(keyPair);
            
            Assert.False(verifier.Verify(signature, message));
        }

        [Fact]
        public void VerifySignature_CorrectDataAndSig_ReturnsTrue()
        {
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            
            byte[] message = new BigInteger("968236873715988614170569073515315707566766479517").ToByteArray();
            
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(keyPair, message);
            
            ECVerifier verifier = new ECVerifier(keyPair);
            
            Assert.True(verifier.Verify(signature, message));
        }
        
        [Fact]
        public void SignatureDecoding_Decode_ShouldBeEqualToOriginal()
        {
            // GenerareceivedKeyte the key pair 
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            
            // Get its byte array representation
            byte[] initialPublicKey = keyPair.GetPublicKey(compressed: true);
            
            // Reconstruct it and check if the key is the same
            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(initialPublicKey);
            byte[] receivedKey = recipientKeyPair.GetPublicKey(true);
            
            Assert.True(receivedKey.SequenceEqual(initialPublicKey));
        }
    }
}
using System.Security.Cryptography;
using AElf.Cryptography.ECDSA;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Xunit;

namespace AElf.Cryptography.Tests.ECDSA
{
    public class EcdsaTest
    {
        [Fact]
        public void VerifySignature_CorrectDataAndSig_ReturnsTrue()
        {
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            
            byte[] message = new BigInteger("968236873715988614170569073515315707566766479517").ToByteArray();
            byte[] msgHash = SHA256.Create().ComputeHash(message);
            
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(keyPair, msgHash);
            
            ECVerifier verifier = new ECVerifier();
            
            Assert.True(verifier.Verify(signature, msgHash));
        }
        
        private static readonly SecureRandom random = new SecureRandom();
    }
}
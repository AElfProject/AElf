using System;
using System.IO;
using System.Linq;
using System.Text;
using AElf.Cryptography.ECDSA;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
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
        
        private static readonly SecureRandom random = new SecureRandom();
        
        [Fact]
        public void SignatureDecoding_Decode_ShouldBeEqualToOriginal()
        {
            // GenerareceivedKeyte the key pair 
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            
            // Get its byte array representation
            byte[] initialPublicKey = keyPair.GetEncodedPublicKey(compressed: true);
            
            // Reconstruct it and check if the key is the same
            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(initialPublicKey);
            byte[] receivedKey = recipientKeyPair.GetEncodedPublicKey(true);
            
            Assert.True(receivedKey.SequenceEqual(initialPublicKey));
        }
        
        [Fact]
        public void xxx()
        {
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            string oo = Convert.ToBase64String(keyPair.GetEncodedPublicKey());
            
            var asym = new AsymmetricCipherKeyPair(keyPair.PublicKey, keyPair.PrivateKey);

            AsymmetricCipherKeyPair p = DoWriteReadTest(keyPair.PrivateKey, "AES-256-CFB", null);
            
            ECKeyPair kp = new ECKeyPair((ECPrivateKeyParameters)p.Private, (ECPublicKeyParameters)p.Public);
            string o = Convert.ToBase64String(kp.GetEncodedPublicKey());

            ;
        }
        
        private class Password
            : IPasswordFinder
        {
            private readonly char[] password;

            public Password(
                char[] word)
            {
                this.password = (char[]) word.Clone();
            }

            public char[] GetPassword()
            {
                return (char[]) password.Clone();
            }
        }
        
        private AsymmetricCipherKeyPair DoWriteReadTest(AsymmetricKeyParameter	akp, string algo, string passeword)
        {
            try
            {
                StringWriter sw = new StringWriter();
                PemWriter pw = new PemWriter(sw);

                pw.WriteObject(akp, algo, "passeword".ToCharArray(), random);
                pw.Writer.Close();

                string data = sw.ToString();

                PemReader pr = new PemReader(new StringReader(data), new Password("passeword".ToCharArray()));

                AsymmetricCipherKeyPair kp = pr.ReadObject() as AsymmetricCipherKeyPair;

                if (kp == null || !kp.Private.Equals(akp))
                {
                    ;
                    return null;
                }
                else
                {
                    ;
                    return kp;
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }
        
        private AsymmetricCipherKeyPair DoWriteReadTest(AsymmetricKeyParameter akp)
        {
            StringWriter sw = new StringWriter();
            PemWriter pw = new PemWriter(sw);

            pw.WriteObject(akp);
            pw.Writer.Close();

            string data = sw.ToString();

            PemReader pr = new PemReader(new StringReader(data));

            AsymmetricCipherKeyPair kp = pr.ReadObject() as AsymmetricCipherKeyPair;

            if (kp == null || !kp.Private.Equals(akp))
            {
               //Fail("Failed to read back test key");
                ;
                return null;
            }
            else
            {
                return kp;
            }
        }
    }
}
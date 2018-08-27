using System.Collections.Generic;
using System.IO;
using AElf.Common.Application;
using AElf.Cryptography.Certificate;
using AElf.Cryptography.ECDSA;
using AElf.Cryptography.SSL;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Xunit;

namespace AElf.Cryptography.Tests.Certificate
{
    public class CertGeneratorTest
    {
        [Fact]
        public void GenerateCert()
        {
            
            /*AElfKeyStore kstore = new AElfKeyStore(ApplicationHelpers.GetDefaultDataDir());
            string pwd = "123";
            var keyPair = kstore.Create(pwd);
            kstore.CreateCertificate(keyPair, pwd, "127.0.0.1");*/
            string test = "Test";
            
            /*IAsymmetricCipherKeyPairGenerator ecKpg = GeneratorUtilities.GetKeyPairGenerator("RSA");
            ecKpg.Init(new KeyGenerationParameters(new SecureRandom(), 2028));
            var eckeypair = ecKpg.GenerateKeyPair();*/
            /*ECKeyGenerationParameters keygenParams 
                = new ECKeyGenerationParameters(ECParameters.DomainParams, ECParameters.SecureRandom);
        
            ECKeyPairGenerator generator = new ECKeyPairGenerator();
            //generator.Init(new KeyGenerationParameters(new SecureRandom(), 256));
            generator.Init(keygenParams);
            var eckeypair = generator.GenerateKeyPair();*/

            var keypair = new RSAKeyPairGenerator().Generate();
            var certGenerator = new CertGenerator(test).SetPublicKey(keypair.PublicKey);
            certGenerator.AddALternativeName("127.0.0.1");
            var cert = certGenerator.Generate(keypair.PrivateKey);
            var path = Path.Combine(ApplicationHelpers.GetDefaultDataDir(), "certs");
            Directory.CreateDirectory(path);
            var i = new SecureRandom().Next();
            string name = "sidechain";
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(path, name + "_cert.pem"), true)) {
                PemWriter pw = new PemWriter(outputFile);
                pw.WriteObject(cert);
                pw.Writer.Close();
            }
            
            using (StreamReader outputFile = new StreamReader(Path.Combine(path, name + "_cert.pem"), true)) {
                PemReader pr = new PemReader(outputFile);
                X509Certificate certificate = (X509Certificate) pr.ReadObject();
                Assert.Equal(keypair.PublicKey, cert.GetPublicKey());
                Assert.Equal(cert.CertificateStructure, certificate.CertificateStructure);
            }
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(path, name + "_key.pem"), true))
            {
                var pw = new PemWriter(outputFile);
                pw.WriteObject(keypair.PrivateKey);
                pw.Writer.Close();
            }
            using (StreamReader outputFile = new StreamReader(Path.Combine(path, name + "_key.pem"), true)) {
                PemReader pr = new PemReader(outputFile);
                AsymmetricCipherKeyPair kp =  pr.ReadObject() as AsymmetricCipherKeyPair;
                Assert.Equal(keypair.PrivateKey, kp.Private);
            }
        }
    }
}
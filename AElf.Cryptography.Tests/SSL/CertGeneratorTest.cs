using System.Collections.Generic;
using System.IO;
using AElf.Common.Application;
using AElf.Cryptography.ECDSA;
using AElf.Cryptography.SSL;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Xunit;

namespace AElf.Cryptography.Tests.SSL
{
    public class CertGeneratorTest
    {
        [Fact]
        public void GenerateCert()
        {
            string test = "Test";
            IAsymmetricCipherKeyPairGenerator rsaKpg = GeneratorUtilities.GetKeyPairGenerator("RSA");
            var random = new SecureRandom();
            rsaKpg.Init(new KeyGenerationParameters(random, 2048));
            
            AElfKeyStore kstore = new AElfKeyStore(ApplicationHelpers.GetDefaultDataDir());
            var keyPair = kstore.Create("123");
            var certGenerator = new CertGenerator(test).SetPublicKey(keyPair.PublicKey);
            certGenerator.AddALternativeName("127.0.0.1");
            var cert = certGenerator.Generate(keyPair.PrivateKey);
            var path = Path.Combine(ApplicationHelpers.GetDefaultDataDir(), "certs");
            Directory.CreateDirectory(path);
            var i = random.Next();
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(path, "test-" + keyPair.GetAddressHex() + "-cert.pem"), true)) {
                PemWriter pw = new PemWriter(outputFile);
                pw.WriteObject(cert);
                pw.Writer.Close();
            }
            
            using (StreamReader outputFile = new StreamReader(Path.Combine(path, "test-" + keyPair.GetAddressHex() + "-cert.pem"), true)) {
                PemReader pr = new PemReader(outputFile);
                X509Certificate certificate = (X509Certificate) pr.ReadObject();
                Assert.Equal(cert.GetPublicKey(), certificate.GetPublicKey());
                Assert.Equal(cert.CertificateStructure, certificate.CertificateStructure);
            }
        }
    }
}
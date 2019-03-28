using System.IO;
using AElf.Cryptography.Certificate;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using Shouldly;
using Xunit;

namespace AElf.Cryptography.Tests.Certificate
{
    public class CertificateStoreTests
    {
        public CertificateStoreTests()
        {
            _certStore = new CertificateStore(TempDir);
        }

        private const string TempDir = "/tmp";
        private readonly CertificateStore _certStore;

        [Fact]
        public void Add_Certificate()
        {
            var name = "testCertificate";
            var certificateContent = "test information about certificate.";
            var result = _certStore.AddCertificate(name, certificateContent);
            result.ShouldBeTrue();

            var certificateContent1 = _certStore.LoadCertificate(name);
            certificateContent1.ShouldBe(certificateContent);
        }

        [Fact]
        public void Certificate_Verification()
        {
            var keyPair = _certStore.WriteKeyAndCertificate("aelf", "192.168.197.39");
            var certificate = _certStore.LoadCertificate("aelf");
            certificate.ShouldNotBe(string.Empty);
            var privateKey = _certStore.LoadKeyStore("aelf");
            privateKey.ShouldNotBe(null);
            privateKey.ShouldNotBe(string.Empty);

            using (var streamReader = new StreamReader(Path.Combine(TempDir, "certs", "aelf.cert.pem")))
            {
                var pr = new PemReader(streamReader);
                var certificate1 = (X509Certificate) pr.ReadObject();
                Assert.Equal(keyPair.PublicKey, certificate1.GetPublicKey());
            }

            Directory.Delete(Path.Combine(TempDir, "certs"), true);
        }

        [Fact]
        public void Generate_Certificate()
        {
            var keypair = new RSAKeyPairGenerator().Generate();
            var certGenerator = new CertGenerator().SetPublicKey(keypair.PublicKey);
            certGenerator.AddAlternativeName("127.0.0.1");
            var cert = certGenerator.Generate(keypair.PrivateKey);
            var certGen = new X509CertificateParser().ReadCertificate(cert.GetEncoded());
            cert.ShouldBe(certGen);
            cert.GetPublicKey().ShouldBe(keypair.PublicKey);
        }

        [Fact]
        public void Get_EncodedPublicKey()
        {
            var generator = new RSAKeyPairGenerator();
            var rsaKeyPair = generator.Generate();
            var encodePublicKey = rsaKeyPair.GetEncodedPublicKey();
            encodePublicKey.ShouldNotBeNull();

            var rsaKeyPair1 = generator.Generate();
            var encodePublicKey1 = rsaKeyPair1.GetEncodedPublicKey();
            encodePublicKey1.ShouldNotBeNull();

            encodePublicKey.ShouldNotBe(encodePublicKey1);
        }
    }
}
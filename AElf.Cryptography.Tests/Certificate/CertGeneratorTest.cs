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
            var keypair = new RSAKeyPairGenerator().Generate();
            var certGenerator = new CertGenerator().SetPublicKey(keypair.PublicKey);
            certGenerator.AddALternativeName("127.0.0.1");
            var cert = certGenerator.Generate(keypair.PrivateKey);
            var cert_gen = new X509CertificateParser().ReadCertificate(cert.GetEncoded());
            Assert.Equal(cert, cert_gen);
            Assert.Equal(keypair.PublicKey, cert.GetPublicKey());
        }
    }
}
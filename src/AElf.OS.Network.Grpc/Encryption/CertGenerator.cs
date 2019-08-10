using System;
using System.Linq;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace AElf.OS.Network.Grpc.Encryption
{
    public class CertGenerator
    {
        private static double DefautIntervalDays { get; } = 365;

        private const string DefaultSubjectName = "aelf";

        private readonly X509V3CertificateGenerator _certificateGenerator;
        private SecureRandom random = new SecureRandom();
        private string SignatureAlgorithm = "SHA256WITHRSA";

        public CertGenerator(double days = 0)
        {
            _certificateGenerator = new X509V3CertificateGenerator();
            _certificateGenerator.SetSignatureAlgorithm(SignatureAlgorithm);

            var subjectDn = new X509Name("CN=" + DefaultSubjectName);
            var issuerDn = new X509Name("CN=" + DefaultSubjectName);
            _certificateGenerator.SetIssuerDN(issuerDn);
            _certificateGenerator.SetSubjectDN(subjectDn);

            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddDays(Math.Abs(days) >= 1 ? days : DefautIntervalDays);
            _certificateGenerator.SetNotBefore(notBefore);
            _certificateGenerator.SetNotAfter(notAfter);
            _certificateGenerator.SetSerialNumber(BigIntegers.CreateRandomInRange(BigInteger.One,
                BigInteger.ValueOf(Int64.MaxValue), random));
        }

        public CertGenerator SetSignatureAlgorithm(string algorithm)
        {
            SignatureAlgorithm = algorithm;
            return this;
        }

        public CertGenerator SetPublicKey(AsymmetricKeyParameter publcKey)
        {
            _certificateGenerator.SetPublicKey(publcKey);
            return this;
        }

        public X509Certificate Generate(AsymmetricKeyParameter privateKey)
        {
            return _certificateGenerator.Generate(privateKey);
        }

        public CertGenerator AddALternativeName(params string[] addresses)
        {
            var generalNames = addresses.Select(addr => new GeneralName(GeneralName.IPAddress, addr)).ToArray();
            generalNames = generalNames.Append(new GeneralName(GeneralName.DnsName, "34.221.101.179")).ToArray();
            GeneralNames subjectAltName = new GeneralNames(generalNames);
            _certificateGenerator.AddExtension(X509Extensions.SubjectAlternativeName, false, subjectAltName);
            return this;
        }
    }
}
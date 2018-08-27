using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace AElf.Cryptography.Certificate
{
    public class CertGenerator
    {
        private readonly X509V3CertificateGenerator _certificateGenerator;
        private string _signatureAlgorithm = "SHA256WITHRSA";
        private const string DefaultSubjectName = "aelf";
        public static double DefautIntervalDays { get; } = 365;
        private SecureRandom random = new SecureRandom();

        public CertGenerator(string name = null, double days = 0)
        {
            _certificateGenerator = new X509V3CertificateGenerator();
            _certificateGenerator.SetSignatureAlgorithm(_signatureAlgorithm);
            var subjectDn = new X509Name("CN=" + DefaultSubjectName);
            var issuerDn = new X509Name("CN=" + name?? DefaultSubjectName);
            _certificateGenerator.SetIssuerDN(issuerDn);
            _certificateGenerator.SetSubjectDN(subjectDn);
            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddDays(Math.Abs(days) >= 1 ?days : DefautIntervalDays);
            _certificateGenerator.SetNotBefore(notBefore);
            _certificateGenerator.SetNotAfter(notAfter);
            _certificateGenerator.SetSerialNumber(BigIntegers.CreateRandomInRange(BigInteger.One,
                BigInteger.ValueOf(Int64.MaxValue), random));
        }

        public CertGenerator SetSignatureAlgorithm(string algorithm)
        {
            _signatureAlgorithm = algorithm;
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
            generalNames = generalNames.Append(new GeneralName(GeneralName.DnsName, "localhost")).ToArray();
            GeneralNames subjectAltName = new GeneralNames(generalNames);
            _certificateGenerator.AddExtension(X509Extensions.SubjectAlternativeName, false, subjectAltName);
            return this;
        }
    }
}
using System;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
namespace AElf.Cryptography.SSL
{
    public class CertGenerator
    {
        private readonly X509V3CertificateGenerator _certificateGenerator;
        private const string SignatureAlgorithm = "SHA256WithRSA";
        private const string DefaultSubjectName = "AElf";
        public static double DefautIntervalDays { get; } = 365;
        private SecureRandom random = new SecureRandom();

        public CertGenerator(string name, double days = 0)
        {
            _certificateGenerator = new X509V3CertificateGenerator();
            var subjectDn = new X509Name(DefaultSubjectName);
            var issuerDn = new X509Name(name);
            
            _certificateGenerator.SetIssuerDN(issuerDn);
            _certificateGenerator.SetSubjectDN(subjectDn);
            
            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddDays(Math.Abs(days) >= 1 ?days : DefautIntervalDays);
            
            _certificateGenerator.SetNotBefore(notBefore);
            _certificateGenerator.SetNotAfter(notAfter);
        }

        public CertGenerator SetSignatureAlgorithm()
        {
            
        }
        
        public X509Certificate Generate(AsymmetricKeyParameter privateKey)
        {
            Asn1SignatureFactory asn1SignatureFactory = new Asn1SignatureFactory(SignatureAlgorithm, privateKey);
            return _certificateGenerator.Generate(asn1SignatureFactory);
        }
    }
}
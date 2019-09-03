using System;
using System.IO;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

namespace AElf.OS.Network.Grpc.Helpers
{
    public static class TlsHelper
    {
//        public static (RSAKeyPair, X509Certificate) BuildKeyAndCertificate(string ipAddress)
//        {
//            // generate key pair
//            var keyPair = GenerateRsaKeyPair();
//            var certGenerator = new CertGenerator().SetPublicKey(keyPair.PublicKey);
//            
//            certGenerator.AddALternativeName(ipAddress);
//            
//            // generate certificate
//            var cert = certGenerator.Generate(keyPair.PrivateKey);
//            
//            return (keyPair, cert);
//        }

        public static AsymmetricCipherKeyPair GenerateRsaKeyPair()
        {
            RsaKeyPairGenerator generator = new RsaKeyPairGenerator();
            generator.Init(new KeyGenerationParameters(new SecureRandom(), 2028));
            
            return generator.GenerateKeyPair();
        }
        
        public static string Serialize(object obj)
        {
            TextWriter textWriter = new StringWriter();
            PemWriter pemWriter = new PemWriter(textWriter);
            pemWriter.WriteObject(obj);
            pemWriter.Writer.Flush();
            
            return textWriter.ToString();
        }
        
        public static X509Certificate GenerateCertificate(X509Name issuer, X509Name subject, 
            AsymmetricKeyParameter issuerPrivate, AsymmetricKeyParameter subjectPublic)
        {
            ISignatureFactory signatureFactory = new Asn1SignatureFactory(
                PkcsObjectIdentifiers.Sha256WithRsaEncryption.ToString(), issuerPrivate);

            var certGenerator = new X509V3CertificateGenerator();
            certGenerator.SetIssuerDN(issuer);
            certGenerator.SetSubjectDN(subject);
            certGenerator.SetSerialNumber(BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), new SecureRandom()));
            certGenerator.SetNotBefore(DateTime.UtcNow.Date);
            certGenerator.SetNotAfter(DateTime.MaxValue);
            certGenerator.SetPublicKey(subjectPublic);

            return certGenerator.Generate(signatureFactory);
        }
    }
}
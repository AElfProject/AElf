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
        private const int RsaKeyLength = 2048;
        
        public static string ObjectToPem(object obj)
        {
            TextWriter textWriter = new StringWriter();
            PemWriter pemWriter = new PemWriter(textWriter);
            pemWriter.WriteObject(obj);
            pemWriter.Writer.Flush();
            
            return textWriter.ToString();
        }

        public static AsymmetricCipherKeyPair GenerateRsaKeyPair()
        {
            RsaKeyPairGenerator generator = new RsaKeyPairGenerator();
            generator.Init(new KeyGenerationParameters(new SecureRandom(), RsaKeyLength));
            
            return generator.GenerateKeyPair();
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
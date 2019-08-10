using System.IO;
using AElf.OS.Network.Grpc.Encryption;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace AElf.OS.Network.Grpc.Helpers
{
    public static class TlsHelper
    {
        public static (RSAKeyPair, X509Certificate) BuildKeyAndCertificate(string ipAddress)
        {
            // generate key pair
            var keyPair = GenerateRsaKeyPair();
            var certGenerator = new CertGenerator().SetPublicKey(keyPair.PublicKey);
            
            certGenerator.AddALternativeName(ipAddress);
            
            // generate certificate
            var cert = certGenerator.Generate(keyPair.PrivateKey);
            
            return (keyPair, cert);
        }

        public static RSAKeyPair GenerateRsaKeyPair()
        {
            RsaKeyPairGenerator generator = new RsaKeyPairGenerator();
            generator.Init(new KeyGenerationParameters(new SecureRandom(), 2028));
            var kp = generator.GenerateKeyPair();
            return new RSAKeyPair(kp.Private, kp.Public);
        }
        
        public static string Serialize(object obj)
        {
            TextWriter textWriter = new StringWriter();
            PemWriter pemWriter = new PemWriter(textWriter);
            pemWriter.WriteObject(obj);
            pemWriter.Writer.Flush();
            
            return textWriter.ToString();
        }
    }
}
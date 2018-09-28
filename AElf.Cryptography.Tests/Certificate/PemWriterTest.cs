using System.IO;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using Xunit;

namespace AElf.Cryptography.Tests.SSL
{
    public class PemWriterTest
    {
        [Fact]
        public void WriterTest()
        {
            string dir = @"/tmp/";
            if(Directory.Exists(Path.Combine(dir, "certs")))
                Directory.Delete(Path.Combine(dir, "certs"), true);
            var certificateStore = new CertificateStore(dir);
            var name = Hash.Generate().ToHex();
            var keyPair =
                certificateStore.WriteKeyAndCertificate("0x32796e95ae7152aa7a554c181d3653c188f1", "192.168.197.11");
            
            using (StreamReader streamReader = new StreamReader(Path.Combine(dir, "certs", "0x32796e95ae7152aa7a554c181d3653c188f1" + ".cert.pem")))
            {
                PemReader pr = new PemReader(streamReader);
                X509Certificate certificate = (X509Certificate) pr.ReadObject();
                Assert.Equal(keyPair.PublicKey, certificate.GetPublicKey());
            }
            Directory.Delete(Path.Combine(dir, "certs"), true);
        }
    }
}
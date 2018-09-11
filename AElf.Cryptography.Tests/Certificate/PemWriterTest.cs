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
                certificateStore.WriteKeyAndCertificate("0xffd2390c07145bad3c40855347596827e873", "192.168.197.15");
            
            using (StreamReader streamReader = new StreamReader(Path.Combine(dir, "certs", "0xffd2390c07145bad3c40855347596827e873" + ".cert.pem")))
            {
                PemReader pr = new PemReader(streamReader);
                X509Certificate certificate = (X509Certificate) pr.ReadObject();
                Assert.Equal(keyPair.PublicKey, certificate.GetPublicKey());
            }
            Directory.Delete(Path.Combine(dir, "certs"), true);
        }
    }
}
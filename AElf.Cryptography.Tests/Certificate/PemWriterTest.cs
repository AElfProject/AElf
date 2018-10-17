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
            string chainIdStr = "0xb84fe09f32ac58fccf8946c42d81532370621acb000554b0e15a0affc6b4502d";
            var certificateStore = new CertificateStore(dir);
                var keyPair =
                    certificateStore.WriteKeyAndCertificate(chainIdStr, "192.168.197.39");
            
            using (StreamReader streamReader = new StreamReader(Path.Combine(dir, "certs", chainIdStr + ".cert.pem")))
            {
                PemReader pr = new PemReader(streamReader);
                X509Certificate certificate = (X509Certificate) pr.ReadObject();
                Assert.Equal(keyPair.PublicKey, certificate.GetPublicKey());
            }
            Directory.Delete(Path.Combine(dir, "certs"), true);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;

namespace AElf.Cryptography.Certificate
{
    public class CertificateStore
    {
        private string _dataDirectory;
        public string FolderName { get; } = "certs";
        public string CertExtension { get; } = ".cert.pem";
        public string KeyExtension { get; } = ".key.pem";


        public CertificateStore(string dataDirectory)
        {
            _dataDirectory = dataDirectory;
        }

        /// <summary>
        /// write certificate and private key
        /// </summary>
        /// <param name="name"> prefix of file </param>
        /// <param name="ipAddress">ip address to be authenticated</param>
        /// <returns></returns>
        public RSAKeyPair WriteKeyAndCertificate(string name, string ipAddress)
        {
            // generate key pair
            var keyPair = new RSAKeyPairGenerator().Generate();
            var certGenerator = GetCertificateGenerator(keyPair);
            
            // Todo: "127.0.0.1" would be removed eventually
            certGenerator.AddALternativeName(ipAddress);
            
            // generate certificate
            var cert = certGenerator.Generate(keyPair.PrivateKey);
            var path = Path.Combine(_dataDirectory, FolderName);
            WriteKeyAndCertificate(cert, path, name, CertExtension);
            WriteKeyAndCertificate(keyPair.PrivateKey, path, name, KeyExtension);
            return keyPair;
        }

        /// <summary>
        /// write certificate
        /// </summary>
        /// <param name="name"> prefix of file </param>
        /// <param name="certificate"> cert content </param>
        /// <returns></returns>
        public bool AddCertificate(string name, string certificate)
        {
            Directory.CreateDirectory(Path.Combine(_dataDirectory, FolderName));
            using (StreamWriter streamWriter = new StreamWriter(Path.Combine(_dataDirectory, FolderName, name + CertExtension)))
            {
                PemWriter pem = new PemWriter(streamWriter);
                try
                {
                    pem.Writer.WriteAsync(certificate);
                    return true;
                }
                finally
                {
                    pem.Writer.Close();
                }
            }
        }
        
        /// <summary>
        /// get certificate with file name prefix
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetCertificate(string name)
        {
            try
            {
                string crt = File.ReadAllText(Path.Combine(_dataDirectory, FolderName, name + CertExtension));
                return crt;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        
        /// <summary>
        /// get private with file name prefix
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetPrivateKey(string name)
        {
            try
            {
                string crt = File.ReadAllText(Path.Combine(_dataDirectory, FolderName, name + KeyExtension));
                return crt;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private CertGenerator GetCertificateGenerator(RSAKeyPair keyPair)
        {
            return  new CertGenerator().SetPublicKey(keyPair.PublicKey);
        }

        private bool WriteKeyAndCertificate(object obj, string dir, string fileName, string extension)
        {
            // create directory if not exists
            Directory.CreateDirectory(dir);
            
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(dir, fileName + extension), true)) {
                PemWriter pw = new PemWriter(outputFile);
                try
                {
                    switch (obj)
                    {
                        case X509Certificate cert:
                            pw.WriteObject(cert);
                            break;
                        case AsymmetricKeyParameter key:
                            pw.WriteObject(key);
                            break;
                    }

                    return true;
                }
                finally
                {
                    pw.Writer.Close();
                }
            }
        }
       
    }
}
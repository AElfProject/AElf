using System;
using System.Collections.Generic;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;

namespace AElf.Cryptography.Certificate
{
    public class CertificateStore : ICertificateStore
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
            certGenerator.AddAlternativeName(ipAddress);
            
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
            //name = PrefixString(name);
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
        public string LoadCertificate(string name)
        {
            return File.ReadAllText(Path.Combine(_dataDirectory, FolderName, name + CertExtension));
        }
        
        /// <summary>
        /// get private with file name prefix
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string LoadKeyStore(string name)
        {
            string crt = File.ReadAllText(Path.Combine(_dataDirectory, FolderName, name + KeyExtension));
            return crt;
        }

        private CertGenerator GetCertificateGenerator(RSAKeyPair keyPair)
        {
            return new CertGenerator().SetPublicKey(keyPair.PublicKey);
        }

        private bool WriteKeyAndCertificate(object obj, string dir, string fileName, string extension)
        {
            // create directory if not exists
            Directory.CreateDirectory(dir);
            //fileName = PrefixString(fileName);
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(dir, fileName + extension), false)) {
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
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    pw.Writer.Close();
                }
            }
        }

        private string PrefixString(string str)
        {
            return str.StartsWith("0x") ? str : "0x" + str;
        }
    }
}
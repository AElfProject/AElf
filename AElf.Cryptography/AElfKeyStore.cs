using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Cryptography.ECDSA.Exceptions;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace AElf.Cryptography
{
    public class AElfKeyStore //: IKeyStore
    {
        private static readonly SecureRandom _random = new SecureRandom();

        private const string KeyFileExtension = ".ak";
        private const string KeyFolderName = "keys";

        private const string _algo = "AES-256-CFB";
        private string _dataDirectory;
        
        public bool IsOpen { get; private set; }

        private List<OpenAccount> _openAccounts;
        
        private TimeSpan _defaultAccountTimeout = TimeSpan.FromSeconds(10);
        
        public AElfKeyStore(string dataDirectory)
        {
            _dataDirectory = dataDirectory;
            _openAccounts = new List<OpenAccount>();
        }

        public void OpenAsync(string address, string password)
        {
            ECKeyPair kp = ReadKeyPairAsync(address, password);
            
            
            OpenAccount acc = new OpenAccount();
            
            Timer t = new Timer(RemoveAccount, acc, _defaultAccountTimeout, _defaultAccountTimeout);
            acc.Timer = t;
            
            _openAccounts.Add(acc);
        }

        private void RemoveAccount(object accObj)
        {
            if (accObj is OpenAccount openAccount)
            {
                openAccount.Close();
            }
        }

        public ECKeyPair Create(string password)
        {
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            WriteKeyPair(keyPair, password);

            return keyPair;
        }

        public List<string> ListAccounts()
        {
            var dir = GetOrCreateKeystoreDir();
            FileInfo[] files = dir.GetFiles("*" + KeyFileExtension);

            return files.Select(f => Path.GetFileNameWithoutExtension(f.Name)).ToList();
        }

        public ECKeyPair ReadKeyPairAsync(string address, string password)
        {
            try
            {
                string keyFilePath = GetKeyFileFullPath(address);

                AsymmetricCipherKeyPair p;
                using (var textReader = File.OpenText(keyFilePath))
                {
                    PemReader pr = new PemReader(textReader, new Password(password.ToCharArray()));
                    p = pr.ReadObject() as AsymmetricCipherKeyPair;
                }

                if (p == null)
                    return null;
                
                ECKeyPair kp = new ECKeyPair((ECPrivateKeyParameters) p.Private, (ECPublicKeyParameters) p.Public);

                return kp;
            }
            catch (FileNotFoundException ex)
            {
                throw new KeyStoreNotFoundException("Keystore file not found.", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new KeyStoreNotFoundException("Invalid keystore path.", ex);
            }
            catch (PemException pemEx)
            {
                throw new InvalidPasswordException("Invalid password.", pemEx);
            }
            catch (Exception e)
            {
                ;
            }

            return null;
        }

        public void WriteKeyPair(ECKeyPair keyPair, string password)
        {
            if (keyPair == null || keyPair.PrivateKey == null || keyPair.PublicKey == null)
                throw new InvalidKeyPairException("Invalid keypair (null reference).", null);
            
            if (string.IsNullOrEmpty(password))
                throw new InvalidPasswordException("Invalid password.", null);
            
            // Ensure path exists
            GetOrCreateKeystoreDir();
            
            string fullPath = null;
            try
            {
                var address = keyPair.GetHexaAddress();
                fullPath = GetKeyFileFullPath(address);
            }
            catch (Exception e)
            {
                throw new InvalidKeyPairException("Could not calculate the address from the keypair.", e);
            }
            
            var akp = new AsymmetricCipherKeyPair(keyPair.PublicKey, keyPair.PrivateKey);
            
            using (StreamWriter writer = File.CreateText(fullPath))
            {
                PemWriter pw = new PemWriter(writer);

                pw.WriteObject(akp, _algo, password.ToCharArray(), _random);
                pw.Writer.Close();
            }
        }
        
        /// <summary>
        /// Return the full path of the files 
        /// </summary>
        internal string GetKeyFileFullPath(string address)
        {
            string dirPath = GetKeystoreDirectoryPath();
            string filePath = Path.Combine(dirPath, address);
            string filePathWithExtension = Path.ChangeExtension(filePath, KeyFileExtension);

            return filePathWithExtension;
        }

        internal DirectoryInfo GetOrCreateKeystoreDir()
        {
            try
            {
                string dirPath = GetKeystoreDirectoryPath();
                return Directory.CreateDirectory(dirPath);
            }
            catch (Exception e)
            {
                throw new KeyStoreNotFoundException("Invalid data directory path", e);
            }
        }

        internal string GetKeystoreDirectoryPath()
        {
            return Path.Combine(_dataDirectory, KeyFolderName);
        }
    }
}
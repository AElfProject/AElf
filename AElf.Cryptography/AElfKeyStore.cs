using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Cryptography.ECDSA.Exceptions;
using Org.BouncyCastle.Crypto;
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
        private readonly string _dataDirectory;

        // IsOpen not used.
        public bool IsOpen { get; private set; }

        private readonly List<OpenAccount> _openAccounts;

        private readonly TimeSpan _defaultTimeoutToClose = TimeSpan.FromMinutes(10);

        public enum Errors
        {
            None = 0,
            AccountAlreadyUnlocked = 1,
            WrongPassword = 2,
            WrongAccountFormat = 3
        }

        public AElfKeyStore(string dataDirectory)
        {
            _dataDirectory = dataDirectory;
            _openAccounts = new List<OpenAccount>();
        }

        private void OpenAsync(string address, string password, TimeSpan? timeoutToClose)
        {
            ECKeyPair kp = ReadKeyPairAsync(address, password);

            OpenAccount acc = new OpenAccount(address);
            acc.KeyPair = kp;

            if (timeoutToClose.HasValue)
            {
                Timer t = new Timer(CloseAccount, acc, timeoutToClose.Value, timeoutToClose.Value);
                acc.CloseTimer = t;
            }

            _openAccounts.Add(acc);
        }

        public Errors OpenAsync(string address, string password, bool withTimeout = true)
        {
            try
            {
                string[] split = address.Split('_');

                if (split.Length != 3)
                    return Errors.WrongAccountFormat;

                if (String.CompareOrdinal(split[0], "ELF") != 0)
                    return Errors.WrongAccountFormat;

                if (split[1].Length != 6)
                    return Errors.WrongAccountFormat;
                
                if (_openAccounts.Any(x => x.AccountName == address))
                    return Errors.AccountAlreadyUnlocked;
                
                if (withTimeout)
                {
                    OpenAsync(address, password, _defaultTimeoutToClose);
                }
                else
                {
                    OpenAsync(address, password, null);
                }
            }
            catch (InvalidPasswordException e)
            {
                return Errors.WrongPassword;
            }

            return Errors.None;
        }

        private void CloseAccount(object accObj)
        {
            if (!(accObj is OpenAccount openAccount)) return;
            openAccount.Close();
            _openAccounts.Remove(openAccount);
        }

        public ECKeyPair GetAccountKeyPair(string address)
        {
            //return _openAccounts.FirstOrDefault(oa => oa.HexPublicKey.Replace("0x", "").Equals(address.Replace("0x", "")))?.KeyPair;
            return _openAccounts.FirstOrDefault(oa => oa.AccountName == address)?.KeyPair;
        }

        public ECKeyPair Create(string password, string chainId)
        {
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            bool res = WriteKeyPair(keyPair, password, chainId);
            return !res ? null : keyPair;
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
                throw new Exception("Unknown error.", e);
            }
        }

        private bool WriteKeyPair(ECKeyPair keyPair, string password, string chainId)
        {
            if (keyPair?.PrivateKey == null || keyPair.PublicKey == null)
                throw new InvalidKeyPairException("Invalid keypair (null reference).", null);

            if (string.IsNullOrEmpty(password))
            {
                // Why here we can just invoke Console.WriteLine? should we use Logger?
                Console.WriteLine("Invalid password.");
                return false;
            }

            // Ensure path exists
            GetOrCreateKeystoreDir();

            string fullPath = null;
            try
            {
                var address = Address.FromPublicKey(ByteArrayHelpers.FromHexString(chainId), keyPair.GetEncodedPublicKey());
                fullPath = GetKeyFileFullPath(address.GetFormatted());
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not calculate the address from the keypair.", e);
                return false;
            }

            var akp = new AsymmetricCipherKeyPair(keyPair.PublicKey, keyPair.PrivateKey);

            using (var writer = File.CreateText(fullPath))
            {
                var pw = new PemWriter(writer);
                pw.WriteObject(akp, _algo, password.ToCharArray(), _random);
                pw.Writer.Close();
            }

            return true;
        }

        /// <summary>
        /// Return the full path of the files 
        /// </summary>
        private string GetKeyFileFullPath(string address)
        {
            var path = GetKeyFileFullPathStrict(address);
            if (File.Exists(path))
            {
                return path;
            }

            return GetKeyFileFullPathStrict(address);
        }

        private string GetKeyFileFullPathStrict(string address)
        {
            string dirPath = GetKeystoreDirectoryPath();
            string filePath = Path.Combine(dirPath, address);
            string filePathWithExtension = Path.ChangeExtension(filePath, KeyFileExtension);

            return filePathWithExtension;
        }

        private DirectoryInfo GetOrCreateKeystoreDir()
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

        private string GetKeystoreDirectoryPath()
        {
            return Path.Combine(_dataDirectory, KeyFolderName);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Cryptography.ECDSA.Exceptions;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace AElf.Cryptography
{
    public class AElfKeyStore : IKeyStore
    {
        private static readonly SecureRandom Random = new SecureRandom();

        private const string KeyFileExtension = ".ak";
        private const string KeyFolderName = "keys";

        private const string _algo = "AES-256-CFB";
        private readonly string _dataDirectory;

        private readonly List<OpenAccount> _openAccounts;
        public TimeSpan DefaultTimeoutToClose = TimeSpan.FromMinutes(10); //in order to customize time setting.

        public enum Errors
        {
            None = 0,
            AccountAlreadyUnlocked = 1,
            WrongPassword = 2,
            WrongAccountFormat = 3,
            AccountFileNotFound = 4
        }

        public AElfKeyStore(string dataDirectory)
        {
            _dataDirectory = dataDirectory;
            _openAccounts = new List<OpenAccount>();
        }

        private async Task OpenAsync(string address, string password, TimeSpan? timeoutToClose)
        {
            var keyPair = await ReadKeyPairAsync(address, password);
            var openAccount = new OpenAccount(address) {KeyPair = keyPair};

            if (timeoutToClose.HasValue)
            {
                var t = new Timer(CloseAccount, openAccount, timeoutToClose.Value, timeoutToClose.Value);
                openAccount.CloseTimer = t;
            }

            _openAccounts.Add(openAccount);
        }

        public async Task<Errors> OpenAsync(string address, string password, bool withTimeout = true)
        {
            try
            {
                if (_openAccounts.Any(x => x.AccountName == address))
                    return Errors.AccountAlreadyUnlocked;

                if (withTimeout)
                {
                    await OpenAsync(address, password, DefaultTimeoutToClose);
                }
                else
                {
                    await OpenAsync(address, password, null);
                }
            }
            catch (InvalidPasswordException)
            {
                return Errors.WrongPassword;
            }
            catch (KeyStoreNotFoundException)
            {
                return Errors.AccountFileNotFound;
            }

            return Errors.None;
        }

        private void CloseAccount(object accountObject)
        {
            if (!(accountObject is OpenAccount openAccount))
                return;
            openAccount.Close();
            _openAccounts.Remove(openAccount);
        }

        public ECKeyPair GetAccountKeyPair(string address)
        {
            return _openAccounts.FirstOrDefault(oa => oa.AccountName == address)?.KeyPair;
        }

        public async Task<ECKeyPair> CreateAsync(string password, string chainId)
        {
            var keyPair = CryptoHelpers.GenerateKeyPair();
            var res = await WriteKeyPairAsync(keyPair, password, chainId);
            return !res ? null : keyPair;
        }

        public async Task<List<string>> ListAccountsAsync()
        {
            var dir = GetOrCreateKeystoreDir();
            var files = dir.GetFiles("*" + KeyFileExtension);

            return await Task.FromResult(files.Select(f => Path.GetFileNameWithoutExtension(f.Name)).ToList());
        }

        public async Task<ECKeyPair> ReadKeyPairAsync(string address, string password)
        {
            try
            {
                var keyFilePath = GetKeyFileFullPath(address);
                AsymmetricCipherKeyPair cypherKeyPair;
                using (var textReader = File.OpenText(keyFilePath))
                {
                    var pr = new PemReader(textReader, new Password(password.ToCharArray()));
                    cypherKeyPair = await Task.FromResult(pr.ReadObject() as AsymmetricCipherKeyPair);
                }

                return cypherKeyPair == null ? null : new ECKeyPair(cypherKeyPair);
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
        }

        private async Task<bool> WriteKeyPairAsync(ECKeyPair keyPair, string password, string chainId)
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

            string fullPath;
            try
            {
                var address = Address.FromPublicKey(keyPair.PublicKey);
                fullPath = GetKeyFileFullPath(address.GetFormatted());
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not calculate the address from the keypair.", e);
                return false;
            }

            var privateKeyParam = new ECPrivateKeyParameters(new BigInteger(1, keyPair.PrivateKey), ECParameters.DomainParams);
            var publicKeyParam = new ECPublicKeyParameters("EC", ECParameters.Curve.Curve.DecodePoint(keyPair.PublicKey), ECParameters.DomainParams);

            var asymmetricCipherKeyPair = new AsymmetricCipherKeyPair(publicKeyParam, privateKeyParam);

            using (var writer = File.CreateText(fullPath))
            {
                var pemWriter = new PemWriter(writer);
                await Task.Run(() =>
                {
                    pemWriter.WriteObject(asymmetricCipherKeyPair, _algo, password.ToCharArray(), Random);
                    pemWriter.Writer.Close();
                });
            }

            return true;
        }

        /// <summary>
        /// Return the full path of the files 
        /// </summary>
        private string GetKeyFileFullPath(string address)
        {
            var path = GetKeyFileFullPathStrict(address);
            return File.Exists(path) ? path : GetKeyFileFullPathStrict(address);
        }

        private string GetKeyFileFullPathStrict(string address)
        {
            var dirPath = GetKeystoreDirectoryPath();
            var filePath = Path.Combine(dirPath, address);
            var filePathWithExtension = Path.ChangeExtension(filePath, KeyFileExtension);
            return filePathWithExtension;
        }

        private DirectoryInfo GetOrCreateKeystoreDir()
        {
            try
            {
                var dirPath = GetKeystoreDirectoryPath();
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Cryptography.ECDSA.Exceptions;
using AElf.OS.Node.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;
using Nethereum.KeyStore;
using Nethereum.KeyStore.Crypto;

namespace AElf.OS.Account.Infrastructure
{
    public class AElfKeyStore : IKeyStore,ISingletonDependency
    {
        private readonly INodeEnvironmentService _nodeEnvironmentService;

        private const string KeyFileExtension = ".json";
        private const string KeyFolderName = "keys";

        private const string _algo = "AES-256-CFB";

        private readonly List<Account> _unlockedAccounts;
        private readonly KeyStoreService _keyStoreService;

        public TimeSpan DefaultTimeoutToClose = TimeSpan.FromMinutes(10); //in order to customize time setting.

        public enum Errors
        {
            None = 0,
            AccountAlreadyUnlocked = 1,
            WrongPassword = 2,
            WrongAccountFormat = 3,
            AccountFileNotFound = 4
        }

        public AElfKeyStore(INodeEnvironmentService nodeEnvironmentService)
        {
            _nodeEnvironmentService = nodeEnvironmentService;
            _unlockedAccounts = new List<Account>();
            _keyStoreService = new KeyStoreService();
        }

        private async Task UnlockAccountAsync(string address, string password, TimeSpan? timeoutToClose)
        {
            var keyPair = await ReadKeyPairAsync(address, password);
            var unlockedAccount = new Account(address) {KeyPair = keyPair};

            if (timeoutToClose.HasValue)
            {
                var t = new Timer(LockAccount, unlockedAccount, timeoutToClose.Value, timeoutToClose.Value);
                unlockedAccount.LockTimer = t;
            }

            _unlockedAccounts.Add(unlockedAccount);
        }

        public async Task<Errors> UnlockAccountAsync(string address, string password, bool withTimeout = true)
        {
            try
            {
                if (_unlockedAccounts.Any(x => x.AccountName == address))
                    return Errors.AccountAlreadyUnlocked;

                if (withTimeout)
                {
                    await UnlockAccountAsync(address, password, DefaultTimeoutToClose);
                }
                else
                {
                    await UnlockAccountAsync(address, password, null);
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

        private void LockAccount(object accountObject)
        {
            if (!(accountObject is Account unlockedAccount))
                return;
            unlockedAccount.Lock();
            _unlockedAccounts.Remove(unlockedAccount);
        }

        public ECKeyPair GetAccountKeyPair(string address)
        {
            return _unlockedAccounts.FirstOrDefault(oa => oa.AccountName == address)?.KeyPair;
        }

        public async Task<ECKeyPair> CreateAccountKeyPairAsync(string password)
        {
            var keyPair = CryptoHelper.GenerateKeyPair();
            var res = await WriteKeyPairAsync(keyPair, password);
            return !res ? null : keyPair;
        }

        public async Task<List<string>> GetAccountsAsync()
        {
            var dir = CreateKeystoreDirectory();
            var files = dir.GetFiles("*" + KeyFileExtension);

            return await Task.Run(() => files.Select(f => Path.GetFileNameWithoutExtension(f.Name)).ToList());
        }

        public async Task<ECKeyPair> ReadKeyPairAsync(string address, string password)
        {
            try
            {
                var keyFilePath = GetKeyFileFullPath(address);
                var privateKey = await Task.Run(() =>
                {
                    using (var textReader = File.OpenText(keyFilePath))
                    {
                        var json = textReader.ReadToEnd();
                        return _keyStoreService.DecryptKeyStoreFromJson(password, json);
                    }
                });

                return CryptoHelper.FromPrivateKey(privateKey);
            }
            catch (FileNotFoundException ex)
            {
                throw new KeyStoreNotFoundException("Keystore file not found.", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new KeyStoreNotFoundException("Invalid keystore path.", ex);
            }
            catch (DecryptionException ex)
            {
                throw new InvalidPasswordException("Invalid password.", ex);
            }
        }

        private async Task<bool> WriteKeyPairAsync(ECKeyPair keyPair, string password)
        {
            if (keyPair?.PrivateKey == null || keyPair.PublicKey == null)
                throw new InvalidKeyPairException("Invalid keypair (null reference).", null);

            // Ensure path exists
            CreateKeystoreDirectory();

            var address = Address.FromPublicKey(keyPair.PublicKey);
            var fullPath = GetKeyFileFullPath(address.GetFormatted());

            await Task.Run(() =>
            {
                using (var writer = File.CreateText(fullPath))
                {
                    var scryptResult = _keyStoreService.EncryptAndGenerateDefaultKeyStoreAsJson(password,
                        keyPair.PrivateKey,
                        address.GetFormatted());
                    writer.Write(scryptResult);
                    writer.Flush();
                }
            });
            
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

        private DirectoryInfo CreateKeystoreDirectory()
        {
            var dirPath = GetKeystoreDirectoryPath();
            return Directory.CreateDirectory(dirPath);
        }

        private string GetKeystoreDirectoryPath()
        {
            return Path.Combine(_nodeEnvironmentService.GetAppDataPath(), KeyFolderName);
        }
    }
}
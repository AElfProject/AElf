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
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Account.Infrastructure
{
    public class AElfKeyStore : IKeyStore,ISingletonDependency
    {
        private readonly INodeEnvironmentService _nodeEnvironmentService;
        
        private readonly SecureRandom Random = new SecureRandom();

        private const string KeyFileExtension = ".ak";
        private const string KeyFolderName = "keys";

        private const string _algo = "AES-256-CFB";

        private readonly List<Account> _unlockedAccounts;
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
                var cypherKeyPair = await Task.Run(() =>
                {
                    using (var textReader = File.OpenText(keyFilePath))
                    {
                        var pr = new PemReader(textReader, new Password(password.ToCharArray()));
                        return pr.ReadObject() as AsymmetricCipherKeyPair;
                    }
                });

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

        private async Task<bool> WriteKeyPairAsync(ECKeyPair keyPair, string password)
        {
            if (keyPair?.PrivateKey == null || keyPair.PublicKey == null)
                throw new InvalidKeyPairException("Invalid keypair (null reference).", null);

            // Ensure path exists
            CreateKeystoreDirectory();
            
            var address = Address.FromPublicKey(keyPair.PublicKey);
            var fullPath = GetKeyFileFullPath(address.GetFormatted());

            var privateKeyParam =
                new ECPrivateKeyParameters(new BigInteger(1, keyPair.PrivateKey), ECParameters.DomainParams);
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
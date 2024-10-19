using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Cryptography.Exceptions;
using AElf.ExceptionHandler;
using AElf.OS.Node.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nethereum.KeyStore;
using Nethereum.KeyStore.Crypto;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Account.Infrastructure;

public partial class AElfKeyStore : IKeyStore, ISingletonDependency
{
    private const string KeyFileExtension = ".json";
    private const string KeyFolderName = "keys";
    private readonly KeyStoreService _keyStoreService;
    private readonly INodeEnvironmentService _nodeEnvironmentService;

    private readonly List<Account> _unlockedAccounts;

    public AElfKeyStore(INodeEnvironmentService nodeEnvironmentService)
    {
        _nodeEnvironmentService = nodeEnvironmentService;
        _unlockedAccounts = new List<Account>();
        _keyStoreService = new KeyStoreService();

        Logger = NullLogger<AElfKeyStore>.Instance;
    }

    public ILogger<AElfKeyStore> Logger { get; set; }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(AElfKeyStore),
        MethodName = nameof(HandleExceptionWhileUnlockingAccount))]
    public async Task<AccountError> UnlockAccountAsync(string address, string password)
    {
        if (_unlockedAccounts.Any(x => x.AccountName == address))
            return AccountError.AccountAlreadyUnlocked;

        var keyPair = await ReadKeyPairAsync(address, password);
        var unlockedAccount = new Account(address) { KeyPair = keyPair };

        _unlockedAccounts.Add(unlockedAccount);
        return AccountError.None;
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

    [ExceptionHandler(typeof(Exception), TargetType = typeof(AElfKeyStore),
        MethodName = nameof(HandleExceptionWhileReadingKeyPair))]
    public async Task<ECKeyPair> ReadKeyPairAsync(string address, string password)
    {
        var keyFilePath = GetKeyFileFullPath(address);
        var privateKey = await Task.Run(() =>
        {
            using var textReader = File.OpenText(keyFilePath);
            var json = textReader.ReadToEnd();
            return _keyStoreService.DecryptKeyStoreFromJson(password, json);
        });

        return CryptoHelper.FromPrivateKey(privateKey);
    }

    private async Task<bool> WriteKeyPairAsync(ECKeyPair keyPair, string password)
    {
        if (keyPair?.PrivateKey == null || keyPair.PublicKey == null)
            throw new InvalidKeyPairException("Invalid keypair (null reference).", null);

        // Ensure path exists
        CreateKeystoreDirectory();

        var address = Address.FromPublicKey(keyPair.PublicKey);
        var fullPath = GetKeyFileFullPath(address.ToBase58());

        await Task.Run(() =>
        {
            using (var writer = File.CreateText(fullPath))
            {
                var scryptResult = _keyStoreService.EncryptAndGenerateDefaultKeyStoreAsJson(password,
                    keyPair.PrivateKey,
                    address.ToBase58());
                writer.Write(scryptResult);
                writer.Flush();
            }
        });

        return true;
    }

    /// <summary>
    ///     Return the full path of the files
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
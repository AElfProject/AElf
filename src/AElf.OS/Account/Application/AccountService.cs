using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Cryptography.ECVRF;
using AElf.Kernel.Account.Application;
using AElf.OS.Account.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Options;

namespace AElf.OS.Account.Application;

public class AccountService : IAccountService
{
    private readonly AccountOptions _accountOptions;
    private readonly IKeyStore _keyStore;

    public AccountService(IKeyStore keyStore, IOptionsSnapshot<AccountOptions> options)
    {
        _keyStore = keyStore;
        _accountOptions = options.Value;
    }

    public async Task<byte[]> SignAsync(byte[] data)
    {
        var signature = CryptoHelper.SignWithPrivateKey((await GetAccountKeyPairAsync()).PrivateKey, data);
        return signature;
    }

    public async Task<byte[]> GetPublicKeyAsync()
    {
        return (await GetAccountKeyPairAsync()).PublicKey;
    }

    public async Task<byte[]> EncryptMessageAsync(byte[] receiverPublicKey, byte[] plainMessage)
    {
        return CryptoHelper.EncryptMessage((await GetAccountKeyPairAsync()).PrivateKey, receiverPublicKey,
            plainMessage);
    }

    public async Task<byte[]> DecryptMessageAsync(byte[] senderPublicKey, byte[] cipherMessage)
    {
        return CryptoHelper.DecryptMessage(senderPublicKey, (await GetAccountKeyPairAsync()).PrivateKey,
            cipherMessage);
    }

    public async Task<byte[]> ECVrfProveAsync(byte[] message)
    {
        var keyPair = await GetAccountKeyPairAsync();
        return CryptoHelper.ECVrfProve(keyPair, message);
    }

    public async Task<Address> GetAccountAsync()
    {
        var publicKey = (await GetAccountKeyPairAsync()).PublicKey;
        return Address.FromPublicKey(publicKey);
    }

    private async Task<ECKeyPair> GetAccountKeyPairAsync()
    {
        var nodeAccount = _accountOptions.NodeAccount;
        var nodePassword = _accountOptions.NodeAccountPassword ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nodeAccount))
        {
            var accountList = await _keyStore.GetAccountsAsync();
            if (accountList.Count == 0)
            {
                var keyPair = await _keyStore.CreateAccountKeyPairAsync(nodePassword);
                nodeAccount = Address.FromPublicKey(keyPair.PublicKey).ToBase58();
            }
            else
            {
                nodeAccount = accountList.First();
            }
        }

        var accountKeyPair = _keyStore.GetAccountKeyPair(nodeAccount);
        if (accountKeyPair == null)
        {
            await _keyStore.UnlockAccountAsync(nodeAccount, nodePassword);
            accountKeyPair = _keyStore.GetAccountKeyPair(nodeAccount);
        }

        return accountKeyPair;
    }
}
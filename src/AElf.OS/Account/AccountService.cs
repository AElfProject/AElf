using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Account.Application;
using Microsoft.Extensions.Options;

namespace AElf.OS.Account
{
    public class AccountService : IAccountService
    {
        private readonly IKeyStore _keyStore;
        private readonly AccountOptions _accountOptions;

        public AccountService(IKeyStore keyStore, IOptionsSnapshot<AccountOptions> options)
        {
            _keyStore = keyStore;
            _accountOptions = options.Value;
        }

        public async Task<byte[]> SignAsync(byte[] data)
        {
            var signature = CryptoHelpers.SignWithPrivateKey((await GetAccountKeyPairAsync()).PrivateKey, data);
            return signature;
        }

        public async Task<byte[]> GetPublicKeyAsync()
        {
            return (await GetAccountKeyPairAsync()).PublicKey;
        }

        public async Task<byte[]> EncryptMessageAsync(byte[] receiverPublicKey, byte[] plainMessage)
        {
             return CryptoHelpers.EncryptMessage((await GetAccountKeyPairAsync()).PrivateKey, receiverPublicKey,
                plainMessage);
        }

        public async Task<byte[]> DecryptMessageAsync(byte[] senderPublicKey, byte[] cipherMessage)
        {
            return CryptoHelpers.DecryptMessage(senderPublicKey, (await GetAccountKeyPairAsync()).PrivateKey,
                cipherMessage);
        }

        public async Task<Address> GetAccountAsync()
        {
            var publicKey = (await GetAccountKeyPairAsync()).PublicKey;
            return Address.FromPublicKey(publicKey);
        }

        private async Task<ECKeyPair> GetAccountKeyPairAsync()
        {
            var accountKeyPair = _keyStore.GetAccountKeyPair(_accountOptions.NodeAccount);
            if (accountKeyPair == null)
            {
                await _keyStore.OpenAsync(_accountOptions.NodeAccount, _accountOptions.NodeAccountPassword, false);
                accountKeyPair = _keyStore.GetAccountKeyPair(_accountOptions.NodeAccount);
            }

            return accountKeyPair;
        }
    }
}
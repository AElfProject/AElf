using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Account;
using AElf.Kernel.Account.Application;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Account
{
    [Dependency(TryRegister = true)]
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

        public async Task<bool> VerifySignatureAsync(byte[] signature, byte[] data, byte[] publicKey)
        {
            var recoverResult = CryptoHelpers.RecoverPublicKey(signature, data, out var recoverPublicKey);

            return recoverResult && publicKey.BytesEqual(recoverPublicKey);
        }

        public async Task<byte[]> GetPublicKeyAsync()
        {
            return (await GetAccountKeyPairAsync()).PublicKey;
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
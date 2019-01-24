using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Account;
using Microsoft.Extensions.Options;

namespace AElf.OS.Account
{
    public class AccountService : IAccountService
    {
        private readonly AElfKeyStore _keyStore;
        private readonly AccountOptions _accountOptions;

        public AccountService(AElfKeyStore keyStore, IOptionsSnapshot<AccountOptions> options)
        {
            _keyStore = keyStore;
            _accountOptions = options.Value;
        }

        public async Task<byte[]> SignAsync(byte[] data)
        {
            var signer = new ECSigner();
            var signature = signer.Sign(await GetAccountKeyPairAsync(), data);

            return signature.SigBytes;
        }

        public async Task<bool> VerifySignatureAsync(byte[] signature, byte[] data)
        {
            var publicKey = (await GetAccountKeyPairAsync()).PublicKey;
            return CryptoHelpers.Verify(signature, data, publicKey);
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
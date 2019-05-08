using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Account.Application
{
    public interface IAccountService
    {
        Task<byte[]> SignAsync(byte[] data);
        Task<bool> VerifySignatureAsync(byte[] signature, byte[] data, byte[] publicKey);
        Task<byte[]> GetPublicKeyAsync();
        Task<byte[]> EncryptMessage(byte[] receiverPublicKey, byte[] plainMessage);
        Task<byte[]> DecryptMessage(byte[] senderPublicKey, byte[] cipherMessage);
    }

    public static class AccountServiceExtensions
    {
        public static async Task<Address> GetAccountAsync(this IAccountService accountService)
        {
            return Address.FromPublicKey(await accountService.GetPublicKeyAsync());
        }
    }

    /// <summary>
    /// Really need this service to provide key pairs during dpos consensus testing.
    /// </summary>
    public class AccountService : IAccountService, ISingletonDependency
    {
        private readonly IECKeyPairProvider _ecKeyPairProvider;

        public AccountService(IECKeyPairProvider ecKeyPairProvider)
        {
            _ecKeyPairProvider = ecKeyPairProvider;
        }

        public Task<byte[]> SignAsync(byte[] data)
        {
            var signature = CryptoHelpers.SignWithPrivateKey(_ecKeyPairProvider.GetECKeyPair().PrivateKey, data);
            return Task.FromResult(signature);
        }

        public Task<bool> VerifySignatureAsync(byte[] signature, byte[] data, byte[] publicKey)
        {
            var recoverResult = CryptoHelpers.RecoverPublicKey(signature, data, out var recoverPublicKey);
            return Task.FromResult(recoverResult && publicKey.BytesEqual(recoverPublicKey));
        }

        public Task<byte[]> GetPublicKeyAsync()
        {
            return Task.FromResult(_ecKeyPairProvider.GetECKeyPair().PublicKey);
        }

        public Task<byte[]> EncryptMessage(byte[] receiverPublicKey, byte[] plainMessage)
        {
            return Task.FromResult(CryptoHelpers.EncryptMessage(_ecKeyPairProvider.GetECKeyPair().PrivateKey,
                receiverPublicKey,
                plainMessage));
        }

        public Task<byte[]> DecryptMessage(byte[] senderPublicKey, byte[] cipherMessage)
        {
            return Task.FromResult(CryptoHelpers.DecryptMessage(senderPublicKey,
                _ecKeyPairProvider.GetECKeyPair().PrivateKey,
                cipherMessage));
        }
    }
}
using System.Threading.Tasks;
using AElf.Types;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Account.Infrastructure;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Account.Application
{
    public interface IAccountService
    {
        Task<byte[]> SignAsync(byte[] data);
        Task<byte[]> GetPublicKeyAsync();
        Task<byte[]> EncryptMessageAsync(byte[] receiverPublicKey, byte[] plainMessage);
        Task<byte[]> DecryptMessageAsync(byte[] senderPublicKey, byte[] cipherMessage);
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
        private readonly IAElfAsymmetricCipherKeyPairProvider _ecKeyPairProvider;

        public AccountService(IAElfAsymmetricCipherKeyPairProvider ecKeyPairProvider)
        {
            _ecKeyPairProvider = ecKeyPairProvider;
        }

        public Task<byte[]> SignAsync(byte[] data)
        {
            var signature = CryptoHelper.SignWithPrivateKey(_ecKeyPairProvider.GetKeyPair().PrivateKey, data);
            return Task.FromResult(signature);
        }


        public Task<byte[]> GetPublicKeyAsync()
        {
            return Task.FromResult(_ecKeyPairProvider.GetKeyPair().PublicKey);
        }

        public Task<byte[]> EncryptMessageAsync(byte[] receiverPublicKey, byte[] plainMessage)
        {
            return Task.FromResult(CryptoHelper.EncryptMessage(_ecKeyPairProvider.GetKeyPair().PrivateKey,
                receiverPublicKey,
                plainMessage));
        }

        public Task<byte[]> DecryptMessageAsync(byte[] senderPublicKey, byte[] cipherMessage)
        {
            return Task.FromResult(CryptoHelper.DecryptMessage(senderPublicKey,
                _ecKeyPairProvider.GetKeyPair().PrivateKey,
                cipherMessage));
        }
    }
}
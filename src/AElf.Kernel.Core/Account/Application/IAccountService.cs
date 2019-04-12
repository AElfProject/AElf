using System.Threading.Tasks;
using AElf.Common;

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
}
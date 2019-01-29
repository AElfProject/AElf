using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Account
{
    public interface IAccountService
    {
        Task<byte[]> SignAsync(byte[] data);
        Task<bool> VerifySignatureAsync(byte[] signature, byte[] data);
        Task<byte[]> GetPublicKeyAsync();
        Task<Address> GetAccountAsync();
    }
}
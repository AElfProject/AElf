using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Account
{
    public interface IAccountService
    {
        Task<byte[]> Sign(byte[] data);
        Task<bool> VerifySignature(byte[] signature, byte[] data);
        Task<byte[]> GetPublicKey();
        Task<Address> GetAccount();
    }
}
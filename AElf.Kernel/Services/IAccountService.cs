using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Services
{
    public interface IAccountService
    {
        Task<byte[]> Sign(byte[] data);
        Task<bool> VerifySignature(byte[] sigBytes, byte[] data);
        Task<byte[]> GetPublicKey();
        Task<Address> GetAddress();
    }
}
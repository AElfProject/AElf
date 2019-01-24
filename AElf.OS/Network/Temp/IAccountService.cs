using System.Threading.Tasks;

namespace AElf.OS.Network.Temp
{
    public interface IAccountService
    {
        Task<byte[]> Sign(byte[] data);
        Task<bool> VerifySignature(byte[] signature, byte[] data);
        Task<byte[]> GetPublicKey();
    }
}
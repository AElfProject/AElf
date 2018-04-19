using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IDataStore
    {
        Task SetData(Hash pointerHash, byte[] data);
        Task<byte[]> GetData(Hash pointerHash);
    }
}
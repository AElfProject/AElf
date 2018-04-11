using AElf.Kernel.Merkle;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IDataProvider
    {
        IDataProvider GetDataProvider(string name);

        Task SetAsync(Hash currentBlockHash, byte[] obj);

        Task<byte[]> GetAsync(Hash blockHash);
    }
}
using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Storages
{
    public interface IBlockBodyStore
    {
        Task InsertAsync(Hash bodyHash, IBlockBody body);
        Task<BlockBody> GetAsync(Hash bodyHash);
    }
}
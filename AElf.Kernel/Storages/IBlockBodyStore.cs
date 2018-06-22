using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IBlockBodyStore
    {
        Task InsertAsync(Hash bodyHash, IBlockBody body);
        Task<BlockBody> GetAsync(Hash bodyHash);
    }
}
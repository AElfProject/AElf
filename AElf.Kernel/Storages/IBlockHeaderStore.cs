using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IBlockHeaderStore
    {
        Task InsertAsync(IBlockHeader block);

        Task<IBlockHeader> GetAsync(Hash blockHash);
    }
}
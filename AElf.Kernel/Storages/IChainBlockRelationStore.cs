using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IChainBlockRelationStore
    {
        Task InsertAsync(Chain chain, Block block);
        Task<Hash> GetAsync(Hash chainHash, long height);
    }
}
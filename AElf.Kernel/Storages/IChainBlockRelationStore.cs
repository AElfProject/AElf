using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IChainBlockRelationStore
    {
        Task InsertAsync(IHash<IChain> chainHash, IHash<IBlock> blockHash, long height);

        Task<IHash<IBlock>> GetAsync(IHash<IChain> chainHash, long height);
    }
}
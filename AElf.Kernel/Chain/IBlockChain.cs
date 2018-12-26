 using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel
{
    public interface IBlockChain : ILightChain
    {
        Task<bool> HasBlock(Hash blockId);
        Task AddBlocksAsync(IEnumerable<IBlock> blocks);
        Task<IBlock> GetBlockByHashAsync(Hash blockId, bool withTransaction=false);
        Task<IBlock> GetBlockByHeightAsync(ulong height, bool withTransaction=false);
        Task<List<Transaction>> RollbackToHeight(ulong height);
        Task RollbackStateForTransactions(IEnumerable<Hash> txIds, Hash disambiguationHash);
    }
}
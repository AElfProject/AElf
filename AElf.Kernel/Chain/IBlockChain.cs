 using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel
{
    public interface IBlockChain : ILightChain
    {
        IBlock CurrentBlock { get; }
        Task<bool> HasBlock(Hash blockId);
        Task AddBlocksAsync(IEnumerable<IBlock> blocks);
        Task<IBlock> GetBlockByHashAsync(Hash blockId);
        Task<IBlock> GetBlockByHeightAsync(ulong height);
        Task<List<Transaction>> RollbackToHeight(ulong height);
    }
}
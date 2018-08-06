using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Kernel
{
    public interface IBlockChain : ILightChain
    {
        IBlock CurrentBlock { get; }
        Task<bool> HasBlock(Hash blockId);
        Task AddBlocksAsync(IEnumerable<IBlock> blocks);
        Task<IBlock> GetBlockByHashAsync(Hash blockId);
        Task<IBlock> GetBlockByHeightAsync(ulong height);
    }
}
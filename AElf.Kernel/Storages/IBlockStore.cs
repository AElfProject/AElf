using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.Storages
{
    public interface IBlockStore
    {
        Task Insert(Block block);

        Task<Block> GetAsync(IHash blockHash);
    }
    
    public class BlockStore : IBlockStore
    {
        private readonly Dictionary<IHash, Block> _blocks = new Dictionary<IHash, Block>();

        public Task Insert(Block block)
        {
            _blocks.Add(new Hash(block.CalculateHash()), block);
            return Task.CompletedTask;
        }

        public Task<Block> GetAsync(IHash blockHash)
        {
            if (_blocks.TryGetValue(blockHash, out var b))
            {
                return Task.FromResult(b);
            }
            
            throw new InvalidOperationException("Cannot find corresponding transaction.");
        }
    }
}
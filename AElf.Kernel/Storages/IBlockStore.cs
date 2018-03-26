using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.Storages
{
    public interface IBlockStore
    {
        Task Insert(IBlock block);

        Task<IBlock> GetAsync(IHash<IBlock> blockHash);
    }
    
    public class BlockStore : IBlockStore
    {
        private readonly Dictionary<IHash, IBlock> _blocks = new Dictionary<IHash, IBlock>();

        public Task Insert(IBlock block)
        {
            _blocks.Add(new Hash<ITransaction>(block.CalculateHash()), block);
            return Task.CompletedTask;
        }

        public Task<IBlock> GetAsync(IHash<IBlock> blockHash)
        {
            foreach (var k in _blocks.Keys)
            {
                if (k.Equals(blockHash))
                {
                    return Task.FromResult(_blocks[k]);
                }
            }
            throw new InvalidOperationException("Cannot find corresponding transaction.");
        }
    }
}
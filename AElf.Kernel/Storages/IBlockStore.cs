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
        private static readonly Dictionary<IHash, IBlock> Blocks = new Dictionary<IHash, IBlock>();

        public Task Insert(IBlock block)
        {
            if (!Validation(block))
            {
                throw new InvalidOperationException("Invalide block.");
            }
            Blocks.Add(new Hash<ITransaction>(block.CalculateHash()), block);
            return Task.CompletedTask;
        }

        public Task<IBlock> GetAsync(IHash<IBlock> blockHash)
        {
            foreach (var k in Blocks.Keys)
            {
                if (k.Equals(blockHash))
                {
                    return Task.FromResult(Blocks[k]);
                }
            }
            throw new InvalidOperationException("Cannot find corresponding transaction.");
        }

        private bool Validation(IBlock block)
        {
            // TODO:
            // Do some check like duplication, 
            return true;
        }
    }
}
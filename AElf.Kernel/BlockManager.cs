using System;
using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class BlockManager: IBlockManager
    {
        private readonly IBlockHeaderStore _blockHeaderStore;

        public BlockManager(IBlockHeaderStore blockHeaderStore)
        {
            _blockHeaderStore = blockHeaderStore;
        }

        public Task<Block> AddBlockAsync(Block block)
        {
            if (!Validation(block))
            {
                throw new InvalidOperationException("Invalide block.");
            }

            _blockHeaderStore.InsertAsync(block.Header);
            return Task.FromResult(block);
        }

        public Task<BlockHeader> GetBlockHeaderAsync(Hash hash)
        {
            return _blockHeaderStore.GetAsync(hash);
        }
        
        /// <summary>
        /// The validation should be done in manager instead of storage.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private bool Validation(Block block)
        {
            // TODO:
            // Do some checks like duplication, 
            return true;
        }
    }
}
using System;
using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class BlockManager: IBlockManager
    {
        private readonly IBlockStore _blockStore;

        public BlockManager(IBlockStore blockStore)
        {
            _blockStore = blockStore;
        }

        public Task<Block> AddBlockAsync(Block block)
        {
            if (!Validation(block))
            {
                throw new InvalidOperationException("Invalide block.");
            }
            _blockStore.Insert(block);
            return Task.FromResult(block);
        }

        public Task<BlockHeader> GetBlockHeaderAsync(Hash chainGenesisBlockHash)
        {
            return Task.FromResult(_blockStore.GetAsync(chainGenesisBlockHash).Result.Header);
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
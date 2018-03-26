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

        public Task<IBlock> AddBlockAsync(IBlock block)
        {
            if (!Validation(block))
            {
                throw new InvalidOperationException("Invalide block.");
            }
            _blockStore.Insert(block);
            return Task.FromResult(block);
        }

        public Task<IBlockHeader> GetBlockHeaderAsync(IHash<IBlock> chainGenesisBlockHash)
        {
            return Task.FromResult(_blockStore.GetAsync(chainGenesisBlockHash).Result.GetHeader());
        }
        
        /// <summary>
        /// The validation should be done in manager instead of storage.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private bool Validation(IBlock block)
        {
            // TODO:
            // Do some checks like duplication, 
            return true;
        }
    }
}
using System;
using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class BlockManager : IBlockManager
    {
        private readonly IBlockHeaderStore _blockHeaderStore;

        private readonly IBlockBodyStore _blockBodyStore;
        
        public BlockManager(IBlockHeaderStore blockHeaderStore, IBlockBodyStore blockBodyStore)
        {
            _blockHeaderStore = blockHeaderStore;
            _blockBodyStore = blockBodyStore;
        }

        public async Task<Block> AddBlockAsync(Block block)
        {
            if (!Validation(block))
            {
                throw new InvalidOperationException("Invalide block.");
            }

            await _blockHeaderStore.InsertAsync(block.Header);
            await _blockBodyStore.InsertAsync(block.Header.MerkleTreeRootOfTransactions, block.Body);
            return block;
        }

        public Task<IBlockHeader> GetBlockHeaderAsync(Hash hash)
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
            // Do some checks like duplication
            return true;
        }
    }
}
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

        public async Task<IBlock> AddBlockAsync(IBlock block)
        {
            if (!Validation(block))
            {
                throw new InvalidOperationException("Invalide block.");
            }

            await _blockHeaderStore.InsertAsync(block.Header);
            await _blockBodyStore.InsertAsync(block.Header.MerkleTreeRootOfTransactions, block.Body);
            return block;
        }
        

        public async Task<IBlockHeader> GetBlockHeaderAsync(Hash hash)
        {
            return await _blockHeaderStore.GetAsync(hash);
        }

        public async Task<IBlockHeader> AddBlockHeaderAsync(IBlockHeader header)
        {
            return await _blockHeaderStore.InsertAsync(header);
        }

        /// <summary>
        /// The validation should be done in manager instead of storage.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private bool Validation(IBlock block)
        {
            // TODO:
            // Do some checks like duplication
            return true;
        }
    }
}
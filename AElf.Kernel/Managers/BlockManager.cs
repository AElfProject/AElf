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
                throw new InvalidOperationException("Invalid block.");
            }

            await _blockHeaderStore.InsertAsync(block.Header);
            await _blockBodyStore.InsertAsync(block.Header.MerkleTreeRootOfTransactions, block.Body);
            return block;
        }


        public async Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash)
        {
            return await _blockHeaderStore.GetAsync(blockHash);
        }

        public async Task<BlockHeader> AddBlockHeaderAsync(BlockHeader header)
        {
            return await _blockHeaderStore.InsertAsync(header);
        }

        public async Task<Block> GetBlockAsync(Hash blockHash)
        {
            var header = await _blockHeaderStore.GetAsync(blockHash);
            var body = await _blockBodyStore.GetAsync(header.MerkleTreeRootOfTransactions);
            return new Block
            {
                Header = header,
                Body = body
            };
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
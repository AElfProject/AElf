using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Kernel.Storages;
using NLog;

namespace AElf.Kernel.Managers
{
    public class BlockManagerBasic : IBlockManagerBasic
    {
        private readonly IBlockHeaderStore _blockHeaderStore;

        private readonly IBlockBodyStore _blockBodyStore;

        // TODO: Replace BlockManager class with this class
        public BlockManagerBasic(IBlockHeaderStore blockHeaderStore, IBlockBodyStore blockBodyStore)
        {
            _blockHeaderStore = blockHeaderStore;
            _blockBodyStore = blockBodyStore;
        }

        public async Task AddBlockHeaderAsync(IBlockHeader header)
        {
            // TODO: Should support interface IBlockHeader
            await _blockHeaderStore.InsertAsync((BlockHeader) header);
        }
        
        public async Task<IBlockHeader> GetBlockHeaderAsync(Hash blockHash)
        {
            return await _blockHeaderStore.GetAsync(blockHash);
        }

        public async Task AddBlockAsync(IBlock block)
        {
            await _blockHeaderStore.InsertAsync(block.Header);
            await _blockBodyStore.InsertAsync(block.Body.GetHash(), block.Body);
        }

        
        public async Task<IBlock> GetBlockAsync(Hash blockHash)
        {
            var header = await _blockHeaderStore.GetAsync(blockHash);
            var body = await _blockBodyStore.GetAsync(header.GetHash().CalculateHashWith(header.MerkleTreeRootOfTransactions));
            return new Block
            {
                Header = header,
                Body = body
            };
        }
    }
}
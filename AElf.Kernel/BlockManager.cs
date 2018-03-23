using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class BlockManager: IBlockManager
    {
        private IBlockStore _blockStore;

        public BlockManager(IBlockStore blockStore)
        {
            _blockStore = blockStore;
        }

        public Task<Block> AddBlockAsync(Block block)
        {
            throw new System.NotImplementedException();
        }

        public Task<BlockHeader> GetBlockHeaderAsync(Hash chainGenesisBlockHash)
        {
            throw new System.NotImplementedException();
        }
    }
}
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

        public Task<IBlock> AddBlockAsync(IBlock block)
        {
            throw new System.NotImplementedException();
        }

        public Task<IBlockHeader> GetBlockHeaderAsync(IHash<IBlock> chainGenesisBlockHash)
        {
            throw new System.NotImplementedException();
        }
    }
}
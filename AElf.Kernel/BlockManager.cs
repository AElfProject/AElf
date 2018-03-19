using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class BlockManager: IBlockManager
    {
        private readonly IBlockStore _blockStore;
        private readonly IChainBlockRelationStore _relationStore;

        public BlockManager(IBlockStore blockStore, IChainBlockRelationStore _relationStore)
        {
            _blockStore = blockStore;
            // TODO:
            // figure out where this should be.
            _relationStore = _relationStore;
        }

        public Task<IBlock> AddBlockAsync(IBlock block)
        {
            _blockStore.Insert(block);
            return Task.FromResult(block);
        }

        public Task<IBlockHeader> GetBlockHeaderAsync(IHash<IBlock> chainGenesisBlockHash)
        {
            throw new System.NotImplementedException();
        }
    }
}
using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class ChainManager : IChainManager
    {
        private readonly IChainStore _chainStore;

        public ChainManager(IChainStore chainStore)
        {
            _chainStore = chainStore;
        }

        public async Task AppendBlockToChainAsync(IChain chain, Block block)
        {
            if (chain.CurrentBlockHeight == 0)
            {
                // empty chain
                chain.CurrentBlockHash = block.GetHash();
            }
            else if (chain.CurrentBlockHash != block.Header.PreviousHash)
            {
                //Block is not connected
            }
            
            chain.UpdateCurrentBlock(block);
            //await _relationStore.InsertAsync(chain, block);
            await _chainStore.UpdateAsync(chain);
        }

        public Task<IChain> GetChainAsync(Hash id)
        {
            return _chainStore.GetAsync(id);
        }

        public Task<IChain> AddChainAsync(Hash chainId)
        {
            return _chainStore.InsertAsync(new Chain(chainId));
        }
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class ChainManager : IChainManager
    {
        private IChainBlockRelationStore _relationStore;

        private IChainStore _chainStore;

        public ChainManager(IChainBlockRelationStore relationStore, IChainStore chainStore)
        {
            _relationStore = relationStore;
            _chainStore = chainStore;
        }

        /// <summary>
        /// Adds the block async, permanent storage is required
        /// </summary>
        /// <returns>The block async.</returns>
        /// <param name="chain">Chain.</param>
        /// <param name="block">Block.</param>
        public async Task AddBlockAsync(IChain chain, IBlock block)
        {
            chain.UpdateCurrentBlock(block);
            await _relationStore.Insert(chain, block, chain.CurrentBlockHeight);
        }

        /// <summary>
        /// return chain by chainId
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public async Task<IChain> GetAsync(IHash<IChain> chainId)
        {
            var chain = await _chainStore.GetAsync(chainId);
            return chain;
        }

    }
}
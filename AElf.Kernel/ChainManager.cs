using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class ChainManager : IChainManager
    {
        private readonly IChainBlockRelationStore _relationStore;

        public ChainManager(IChainBlockRelationStore relationStore)
        {
            _relationStore = relationStore;
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
            await _relationStore.InsertAsync(
                new Hash<IChain>(chain.CalculateHash()),
                new Hash<IBlock>(block.CalculateHash()), 
                chain.CurrentBlockHeight);
        }
    }
}
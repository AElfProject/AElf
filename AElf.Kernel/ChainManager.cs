﻿using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class ChainManager : IChainManager
    {
        private readonly IChainBlockRelationStore _relationStore;

        private readonly IChainStore _chainStore;

        public ChainManager(IChainBlockRelationStore relationStore, IChainStore chainStore)
        {
            _relationStore = relationStore;
            _chainStore = chainStore;
        }


        public async Task AppenBlockToChainAsync(Chain chain, Block block)
        {
            if (chain.CurrentBlockHash != block.Header.PreviousHash)
            {
                //Block is not connected
            }
            
            chain.UpdateCurrentBlock(block);
            await _relationStore.Insert(chain, block, chain.CurrentBlockHeight);
            await _chainStore.UpdateAsync(chain);

        }

        public Task<Chain> GetChainAsync(Hash id)
        {
            return _chainStore.GetAsync(id);
        }

        public Task<Chain> AddChainAsync(Hash chainId)
        {
            return _chainStore.InsertAsync(new Chain(chainId));
        }
    }
}
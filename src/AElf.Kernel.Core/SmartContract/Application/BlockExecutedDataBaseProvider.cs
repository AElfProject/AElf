using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Application
{
    public abstract class BlockExecutedDataBaseProvider<T>
    {
        private readonly ICachedBlockchainExecutedDataService<T>
            _cachedBlockchainExecutedDataService;

        protected BlockExecutedDataBaseProvider(ICachedBlockchainExecutedDataService<T> cachedBlockchainExecutedDataService)
        {
            _cachedBlockchainExecutedDataService = cachedBlockchainExecutedDataService;
        }

        protected T GetBlockExecutedData(IBlockIndex chainContext, IMessage key = null)
        {
            return _cachedBlockchainExecutedDataService.GetBlockExecutedData(chainContext, GetBlockExecutedDataKey(key));
        }
        
        protected T GetBlockExecutedData(IBlockIndex chainContext, string key)
        {
            return _cachedBlockchainExecutedDataService.GetBlockExecutedData(chainContext, GetBlockExecutedDataKey(key));
        }

        protected async Task AddBlockExecutedDataAsync(IBlockIndex blockIndex, IMessage key, T blockExecutedData)
        {
            await _cachedBlockchainExecutedDataService.AddBlockExecutedDataAsync(blockIndex,
                GetBlockExecutedDataKey(key), blockExecutedData);
        }
        
        protected async Task AddBlockExecutedDataAsync(IBlockIndex blockIndex, string key, T blockExecutedData)
        {
            await _cachedBlockchainExecutedDataService.AddBlockExecutedDataAsync(blockIndex,
                GetBlockExecutedDataKey(key), blockExecutedData);
        }
        
        protected async Task AddBlockExecutedDataAsync(IBlockIndex blockIndex, T blockExecutedData)
        {
            await _cachedBlockchainExecutedDataService.AddBlockExecutedDataAsync(blockIndex,
                GetBlockExecutedDataKey(), blockExecutedData);
        }

        protected async Task AddBlockExecutedDataAsync<TKey>(IBlockIndex blockIndex, IDictionary<TKey, T> blockExecutedData)
            where TKey : IMessage<TKey>
        {
            var dic = blockExecutedData.ToDictionary(pair => GetBlockExecutedDataKey(pair.Key), pair => pair.Value);
            await _cachedBlockchainExecutedDataService.AddBlockExecutedDataAsync(blockIndex, dic);
        }

        protected abstract string GetBlockExecutedDataName();
            
        private string GetBlockExecutedDataKey(object key = null)
        {
            var list = new List<string> {KernelConstants.BlockExecutedDataKey, GetBlockExecutedDataName()};
            if(key != null) list.Add(key.ToString());
            return string.Join("/", list);
        }
    }
}
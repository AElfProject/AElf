using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChain.Cache;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.CrossChain
{
    public class CrossChainDataProvider : ICrossChainDataProvider, ISingletonDependency, ILocalEventHandler<NewIrreversibleBlockFoundEvent>
    {
        private readonly ICrossChainContractReader _crossChainContractReader;
        private readonly ICrossChainDataConsumer _crossChainDataConsumer;
        public ILogger<CrossChainDataProvider> Logger { get; set; }

        private readonly Dictionary<Hash, CrossChainBlockData> _indexedCrossChainBlockData =
            new Dictionary<Hash, CrossChainBlockData>();
        private KeyValuePair<long, Hash> _libHeightToHash;
        public CrossChainDataProvider(ICrossChainContractReader crossChainContractReader,
            ICrossChainDataConsumer crossChainDataConsumer, ILocalLibService localLibService)
        {
            _crossChainContractReader = crossChainContractReader;
            _crossChainDataConsumer = crossChainDataConsumer;
        }

        public async Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight)
        {
            var sideChainBlockData = new List<SideChainBlockData>(); 
            var dict = await _crossChainContractReader.GetSideChainIdAndHeightAsync(currentBlockHash,
                currentBlockHeight);
            foreach (var idHeight in dict)
            {
                Logger.LogTrace($"Side chain id {idHeight.Key}");
                // index only one block from one side chain which could be changed later.
                // cause take these data before mining, the target height of consumer == height + 1
                var blockInfo = _crossChainDataConsumer.TryTake<SideChainBlockData>(idHeight.Key, idHeight.Value + 1, true);
                if (blockInfo == null)
                    continue;
                Logger.LogTrace($"With Height {blockInfo.Height}");
                sideChainBlockData.Add(blockInfo);
            }

            Logger.LogTrace($"Side chain block data count {sideChainBlockData.Count}");
            return sideChainBlockData;
        }

        public async Task<bool> ValidateSideChainBlockDataAsync(List<SideChainBlockData> sideChainBlockData, 
            Hash currentBlockHash, long currentBlockHeight)
        {
            foreach (var blockInfo in sideChainBlockData)
            {
                // this happens after block execution
                // cause take these data after block execution, the target height of consumer == height.
                // return 0 if side chain not exist.
                var targetHeight =
                    await _crossChainContractReader.GetSideChainCurrentHeightAsync(blockInfo.ChainId, currentBlockHash,
                        currentBlockHeight) + 1;
                if (targetHeight != blockInfo.Height)
                    // this should not happen if it is good data.
                    return false;

                var cachedBlockInfo = _crossChainDataConsumer.TryTake<SideChainBlockData>(blockInfo.ChainId, targetHeight, false);
                if (cachedBlockInfo == null || !cachedBlockInfo.Equals(blockInfo))
                    return false;
            }

            return true;
        }

        public async Task<List<ParentChainBlockData>> GetParentChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight)
        {
            var parentChainBlockData = new List<ParentChainBlockData>();
            var parentChainId = await _crossChainContractReader.GetParentChainIdAsync(currentBlockHash, currentBlockHeight);
            if (parentChainId == 0)
            {
                //Logger.LogTrace("No configured parent chain");
                // no configured parent chain
                return parentChainBlockData;
            }
                
            const int length = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            var heightInState =
                await _crossChainContractReader.GetParentChainCurrentHeightAsync(currentBlockHash, currentBlockHeight);
            
            var targetHeight = heightInState + 1;
            Logger.LogTrace($"Target height {targetHeight}");

            var i = 0;
            while (i < length)
            {

                var blockInfo = _crossChainDataConsumer.TryTake<ParentChainBlockData>(parentChainId, targetHeight, true);
                if (blockInfo == null)
                {
                    // no more available parent chain block info
                    break;
                }

                parentChainBlockData.Add(blockInfo);
                Logger.LogTrace($"Got parent chain height {blockInfo.Height}");
                targetHeight++;
                i++;
            }
            Logger.LogTrace($"Parent chain block data count {parentChainBlockData.Count}");
            return parentChainBlockData;

        }

        public async Task<bool> ValidateParentChainBlockDataAsync(List<ParentChainBlockData> parentChainBlockData, 
            Hash currentBlockHash, long currentBlockHeight)
        {
            if (parentChainBlockData.Count == 0)
                return true;
            var parentChainId = await _crossChainContractReader.GetParentChainIdAsync(currentBlockHash, currentBlockHeight);
            if (parentChainId == 0)
                // no configured parent chain
                return false;

            var length = parentChainBlockData.Count;

            if (length > CrossChainConsts.MaximalCountForIndexingParentChainBlock)
                return false;

            var i = 0;

            var targetHeight =
                await _crossChainContractReader.GetParentChainCurrentHeightAsync(currentBlockHash, currentBlockHeight) + 1;
            var res = true;
            while (i < length)
            {
                var blockInfo = _crossChainDataConsumer.TryTake<ParentChainBlockData>(parentChainId, targetHeight, false);
                if (blockInfo == null)
                {
                    // no more available parent chain block info
                    res = false;
                    break;
                }
                    
                if (!parentChainBlockData[i].Equals(blockInfo))
                    // cached parent chain block info is not compatible with provided.
                    return false;
                targetHeight++;
                i++;
            }

            return res;
        }

        public void RegisterNewChain(int chainId)
        {
            _crossChainDataConsumer.TryRegisterNewChainCache(chainId);
        }

        public async Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight)
        {
            return await _crossChainContractReader.GetIndexedCrossChainBlockDataAsync(currentBlockHash, currentBlockHeight);
        }
        
        /// <summary>
        /// This method returns cross chain data.
        /// </summary>
        /// <param name="currentBlockHash"></param>
        /// <param name="currentBlockHeight"></param>
        /// <returns></returns>
        public async Task<CrossChainBlockData> GetCrossChainBlockDataForNextMiningAsync(Hash currentBlockHash, long currentBlockHeight)
        {
            var sideChainBlockData = await GetSideChainBlockDataAsync(currentBlockHash, currentBlockHeight);
            var parentChainBlockData = await GetParentChainBlockDataAsync(currentBlockHash, currentBlockHeight);

            if (sideChainBlockData.Count == 0 && parentChainBlockData.Count == 0)
                return null;
            var crossChainBlockData = new CrossChainBlockData();
            crossChainBlockData.ParentChainBlockData.AddRange(parentChainBlockData);
            crossChainBlockData.SideChainBlockData.AddRange(sideChainBlockData);
            crossChainBlockData.PreviousBlockHeight = currentBlockHeight;
            _indexedCrossChainBlockData[currentBlockHash] = crossChainBlockData;
            Logger.LogTrace($"IndexedCrossChainBlockData count {_indexedCrossChainBlockData.Count}");
            return crossChainBlockData;
        }

        /// <summary>
        /// This method returns cross chain data already used before.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <param name="previousBlockHeight"></param>
        /// <returns></returns>
        public CrossChainBlockData GetUsedCrossChainBlockDataForLastMiningAsync(Hash blockHash, long previousBlockHeight)
        {
            Logger.LogTrace($"IndexedCrossChainBlockData count {_indexedCrossChainBlockData.Count}");
            return _indexedCrossChainBlockData.TryGetValue(blockHash, out var crossChainBlockData)
                ? crossChainBlockData
                : null;
        }

        public async Task<ChainInitializationContext> GetChainInitializationContextAsync(int chainId)
        {
            if (_libHeightToHash.Value != null)
                return await _crossChainContractReader.GetChainInitializationContextAsync(_libHeightToHash.Value,
                    _libHeightToHash.Key, chainId);
            return null;
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            // create cache for new chain
            var dict = await _crossChainContractReader.GetAllChainsIdAndHeightAsync(eventData.BlockHash, eventData.BlockHeight);
            foreach (var chainIdHeight in dict)
            {
                _crossChainDataConsumer.TryRegisterNewChainCache(chainIdHeight.Key, chainIdHeight.Value + 1);
            }
            // clear useless cache
            var toRemoveList = _indexedCrossChainBlockData.Where(kv => kv.Value.PreviousBlockHeight + 1 < eventData.BlockHeight)
                .Select(kv => kv.Key).ToList();
            foreach (var hash in toRemoveList)
            {
                _indexedCrossChainBlockData.Remove(hash);
            }

            _libHeightToHash = new KeyValuePair<long, Hash>(eventData.BlockHeight, eventData.BlockHash);
        }
    }
}
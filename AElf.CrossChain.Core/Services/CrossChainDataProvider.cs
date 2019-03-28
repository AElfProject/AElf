using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain.Cache;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Domain;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class CrossChainDataProvider : ICrossChainDataProvider, ISingletonDependency
    {
        private readonly ICrossChainContractReader _crossChainContractReader;
        private readonly ICrossChainDataConsumer _crossChainDataConsumer;
        private readonly ILocalLibService _localLibService;
        public ILogger<CrossChainDataProvider> Logger { get; set; }

        private readonly Dictionary<Hash, CrossChainBlockData> _indexedCrossChainBlockData = new Dictionary<Hash, CrossChainBlockData>();
        public CrossChainDataProvider(ICrossChainContractReader crossChainContractReader,
            ICrossChainDataConsumer crossChainDataConsumer, ILocalLibService localLibService)
        {
            _crossChainContractReader = crossChainContractReader;
            _crossChainDataConsumer = crossChainDataConsumer;
            _localLibService = localLibService;
        }

        public async Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(Hash previousBlockHash, long preBlockHeight)
        {
            var sideChainBlockData = new List<SideChainBlockData>(); 
            var dict = await _crossChainContractReader.GetSideChainIdAndHeightAsync(previousBlockHash,
                preBlockHeight);
            foreach (var idHeight in dict)
            {
                Logger.LogTrace($"Side chain id {idHeight.Key}");
                // index only one block from one side chain which could be changed later.
                // cause take these data before mining, the target height of consumer == height + 1
                var blockInfo = _crossChainDataConsumer.TryTake(idHeight.Key, idHeight.Value + 1, true);
                if (blockInfo == null)
                    continue;
                Logger.LogTrace($"With Height {blockInfo.Height}");
                sideChainBlockData.Add((SideChainBlockData) blockInfo);
            }

            Logger.LogTrace($"Side chain block data count {sideChainBlockData.Count}");
            return sideChainBlockData;
        }

        public async Task<bool> ValidateSideChainBlockDataAsync(List<SideChainBlockData> sideChainBlockData, 
            Hash previousBlockHash, long preBlockHeight)
        {
            foreach (var blockInfo in sideChainBlockData)
            {
                // this happens after block execution
                // cause take these data after block execution, the target height of consumer == height.
                // return 0 if side chain not exist.
                var targetHeight =
                    await _crossChainContractReader.GetSideChainCurrentHeightAsync(blockInfo.ChainId, previousBlockHash,
                        preBlockHeight) + 1;
                if (targetHeight != blockInfo.Height)
                    // this should not happen if it is good data.
                    return false;

                var cachedBlockInfo = _crossChainDataConsumer.TryTake(blockInfo.ChainId, targetHeight, false);
                if (cachedBlockInfo == null || !cachedBlockInfo.Equals(blockInfo))
                    return false;
            }

            return true;
        }

        public async Task<List<ParentChainBlockData>> GetParentChainBlockDataAsync(Hash previousBlockHash, long preBlockHeight)
        {
            var parentChainBlockData = new List<ParentChainBlockData>();
            var parentChainId = await _crossChainContractReader.GetParentChainIdAsync(previousBlockHash, preBlockHeight);
            if (parentChainId == 0)
            {
                //Logger.LogTrace("No configured parent chain");
                // no configured parent chain
                return parentChainBlockData;
            }
                
            const int length = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            var heightInState =
                await _crossChainContractReader.GetParentChainCurrentHeightAsync(previousBlockHash, preBlockHeight);
            
            var targetHeight = heightInState + 1;
            Logger.LogTrace($"Target height {targetHeight}");

            var i = 0;
            while (i < length)
            {

                var blockInfo = _crossChainDataConsumer.TryTake(parentChainId, targetHeight, true);
                if (blockInfo == null)
                {
                    // no more available parent chain block info
                    break;
                }

                parentChainBlockData.Add((ParentChainBlockData) blockInfo);
                Logger.LogTrace($"Got parent chain height {blockInfo.Height}");
                targetHeight++;
                i++;
            }
            Logger.LogTrace($"Parent chain block data count {parentChainBlockData.Count}");
            return parentChainBlockData;

        }

        public async Task<bool> ValidateParentChainBlockDataAsync(List<ParentChainBlockData> parentChainBlockData, 
            Hash previousBlockHash, long preBlockHeight)
        {
            if (parentChainBlockData.Count == 0)
                return true;
            var parentChainId = await _crossChainContractReader.GetParentChainIdAsync(previousBlockHash, preBlockHeight);
            if (parentChainId == 0)
                // no configured parent chain
                return false;

            var length = parentChainBlockData.Count();

            if (length > CrossChainConsts.MaximalCountForIndexingParentChainBlock)
                return false;

            var i = 0;

            var targetHeight =
                await _crossChainContractReader.GetParentChainCurrentHeightAsync(previousBlockHash, preBlockHeight) + 1;
            var res = true;
            while (i < length)
            {
                var blockInfo = _crossChainDataConsumer.TryTake(parentChainId, targetHeight, false);
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

        public async Task ActivateCrossChainCacheAsync(Hash blockHash, long blockHeight)
        {
            var dict = await _crossChainContractReader.GetAllChainsIdAndHeightAsync(blockHash, blockHeight);
            foreach (var chainIdHeight in dict)
            {
                _crossChainDataConsumer.TryRegisterNewChainCache(chainIdHeight.Key, chainIdHeight.Value + 1);
            }
        }

        public void RegisterNewChain(int chainId)
        {
            _crossChainDataConsumer.TryRegisterNewChainCache(chainId);
        }

        public async Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash previousBlockHash, long previousBlockHeight)
        {
            return await _crossChainContractReader.GetIndexedCrossChainBlockDataAsync(previousBlockHash, previousBlockHeight);
        }

        /// <summary>
        /// This method returns cross chain data.
        /// </summary>
        /// <param name="previousBlockHash"></param>
        /// <param name="previousBlockHeight"></param>
        /// <returns></returns>
        public async Task<CrossChainBlockData> GetNewCrossChainBlockDataAsync(Hash previousBlockHash, long previousBlockHeight)
        {
            var height = await _localLibService.GetLibHeight();
            var toRemoveList = _indexedCrossChainBlockData.Where(kv => kv.Value.PreviousBlockHeight + 1 < height)
                .Select(kv => kv.Key).ToList();
            foreach (var hash in toRemoveList)
            {
                _indexedCrossChainBlockData.Remove(hash);
            }
            var sideChainBlockData = await GetSideChainBlockDataAsync(previousBlockHash, previousBlockHeight);
            var parentChainBlockData = await GetParentChainBlockDataAsync(previousBlockHash, previousBlockHeight);

            if (sideChainBlockData.Count == 0 && parentChainBlockData.Count == 0)
                return null;
            var crossChainBlockData = new CrossChainBlockData();
            crossChainBlockData.ParentChainBlockData.AddRange(parentChainBlockData);
            crossChainBlockData.SideChainBlockData.AddRange(sideChainBlockData);
            crossChainBlockData.PreviousBlockHeight = previousBlockHeight;
            _indexedCrossChainBlockData[previousBlockHash] = crossChainBlockData;
            Logger.LogTrace($"IndexedCrossChainBlockData count {_indexedCrossChainBlockData.Count}");
            return crossChainBlockData;
        }

        /// <summary>
        /// This method returns cross chain data already used before.
        /// </summary>
        /// <param name="previousBlockHash"></param>
        /// <param name="previousBlockHeight"></param>
        /// <returns></returns>
        public CrossChainBlockData GetUsedCrossChainBlockData(Hash previousBlockHash, long previousBlockHeight)
        {
            Logger.LogTrace($"IndexedCrossChainBlockData count {_indexedCrossChainBlockData.Count}");
            return _indexedCrossChainBlockData.TryGetValue(previousBlockHash, out var crossChainBlockData)
                ? crossChainBlockData
                : null;
        }

    }
}
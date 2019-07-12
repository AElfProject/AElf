using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache.Application;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    internal class CrossChainDataProvider : ICrossChainDataProvider, ISingletonDependency
    {
        private readonly IReaderFactory _readerFactory;
        
        private readonly IBlockCacheEntityConsumer _blockCacheEntityConsumer;
        public ILogger<CrossChainDataProvider> Logger { get; set; }

        private readonly Dictionary<Hash, CrossChainBlockData> _indexedCrossChainBlockData =
            new Dictionary<Hash, CrossChainBlockData>();

        public IOptionsSnapshot<CrossChainConfigOptions> CrossChainOptions { get; set; }
        
        public CrossChainDataProvider(IReaderFactory readerFactory, IBlockCacheEntityConsumer blockCacheEntityConsumer)
        {
            _readerFactory = readerFactory;
            _blockCacheEntityConsumer = blockCacheEntityConsumer;
        }

        public async Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight)
        {
            var sideChainBlockDataList = new List<SideChainBlockData>();
            var sideChainIndexingInformationList = await _readerFactory.Create(currentBlockHash, currentBlockHeight)
                .GetSideChainIndexingInformationList.CallAsync(new Empty());
            foreach (var sideChainIndexingInformation in sideChainIndexingInformationList.IndexingInformationList)
            {
                var targetHeight = sideChainIndexingInformation.IndexedHeight + 1;
                var toBeIndexedCount = Math.Min(CrossChainOptions.Value.MaximalCountForIndexingSideChainBlock,
                    sideChainIndexingInformation.ToBeIndexedCount);

                Logger.LogTrace(
                    $"Target height {targetHeight} of side chain " +
                    $"{ChainHelper.ConvertChainIdToBase58(sideChainIndexingInformation.ChainId)}.");
                var sideChainBlockDataFromCache = new List<SideChainBlockData>();  
                
                var i = 0;
                while (i < toBeIndexedCount)
                {
                    var sideChainBlockData =
                        _blockCacheEntityConsumer.Take<SideChainBlockData>(sideChainIndexingInformation.ChainId,
                            targetHeight, true);
                    if (sideChainBlockData == null)
                    {
                        // no more available parent chain block info
                        break;
                    }
                    
                    sideChainBlockDataFromCache.Add(sideChainBlockData);
                    targetHeight++;
                    i++;
                }

                if (sideChainBlockDataFromCache.Count > 0)
                {
                    Logger.LogTrace(
                        $"Got height [{sideChainBlockDataFromCache.First().Height} - {sideChainBlockDataFromCache.Last().Height} ]" +
                        $" from side chain {ChainHelper.ConvertChainIdToBase58(sideChainIndexingInformation.ChainId)}.");
                    sideChainBlockDataList.AddRange(sideChainBlockDataFromCache);
                }
            }

            return sideChainBlockDataList;
        }

        public async Task<bool> ValidateSideChainBlockDataAsync(List<SideChainBlockData> sideChainBlockDataList, 
            Hash currentBlockHash, long currentBlockHeight)
        {
            var sideChainValidatedHeightDict = new Dictionary<int, long>(); // chain id => validated height
            foreach (var sideChainBlockData in sideChainBlockDataList)
            {
                if (!sideChainValidatedHeightDict.TryGetValue(sideChainBlockData.ChainId, out var validatedHeight))
                {
                    var height = await _readerFactory.Create(currentBlockHash, currentBlockHeight).GetSideChainHeight
                        .CallAsync(
                            new SInt32Value()
                            {
                                Value = sideChainBlockData.ChainId
                            });
                    validatedHeight = height?.Value ?? 0;
                }

                long targetHeight = validatedHeight + 1; 

                if (targetHeight != sideChainBlockData.Height)
                    // this should not happen if it is good data.
                    return false;

                var cachedSideChainBlockData =
                    _blockCacheEntityConsumer.Take<SideChainBlockData>(sideChainBlockData.ChainId, targetHeight, false);
                if (cachedSideChainBlockData == null)
                    throw new ValidateNextTimeBlockValidationException(
                        $"Side chain data not found, chainId: {ChainHelper.ConvertChainIdToBase58(sideChainBlockData.ChainId)}, side chain height: {targetHeight}.");
                if (!cachedSideChainBlockData.Equals(sideChainBlockData))
                    return false;
                
                sideChainValidatedHeightDict[sideChainBlockData.ChainId] = sideChainBlockData.Height;
            }

            return true;
        }

        public async Task<List<ParentChainBlockData>> GetParentChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight)
        {
            var parentChainBlockDataList = new List<ParentChainBlockData>();
            var returnValue = await _readerFactory.Create(currentBlockHash, currentBlockHeight).GetParentChainId
                .CallAsync(new Empty());
            var parentChainId = returnValue?.Value ?? 0;
            if (parentChainId == 0)
            {
                //Logger.LogTrace("No configured parent chain");
                // no configured parent chain
                return parentChainBlockDataList;
            }
                
            int length = CrossChainOptions.Value.MaximalCountForIndexingParentChainBlock;
            var heightInState = (await _readerFactory.Create(currentBlockHash, currentBlockHeight).GetParentChainHeight
                .CallAsync(new Empty())).Value;
            
            var targetHeight = heightInState + 1;
            Logger.LogTrace($"Target height {targetHeight}");

            var i = 0;
            while (i < length)
            {
                var parentChainBlockData = _blockCacheEntityConsumer.Take<ParentChainBlockData>(parentChainId, targetHeight, true);
                if (parentChainBlockData == null)
                {
                    // no more available parent chain block info
                    break;
                }

                parentChainBlockDataList.Add(parentChainBlockData);
                targetHeight++;
                i++;
            }

            if (parentChainBlockDataList.Count > 0)
                Logger.LogTrace(
                    $"Got height [{parentChainBlockDataList.First().Height} - {parentChainBlockDataList.Last().Height} ]" +
                    $" from parent chain {ChainHelper.ConvertChainIdToBase58(parentChainId)}.");
            return parentChainBlockDataList;
        }

        public async Task<bool> ValidateParentChainBlockDataAsync(List<ParentChainBlockData> parentChainBlockDataList, 
            Hash currentBlockHash, long currentBlockHeight)
        {
            if (parentChainBlockDataList.Count == 0)
                return true;
            var parentChainId = (await _readerFactory.Create(currentBlockHash, currentBlockHeight).GetParentChainId
                .CallAsync(new Empty())).Value;
            if (parentChainId == 0)
                // no configured parent chain
                return false;

            var length = parentChainBlockDataList.Count;

            var i = 0;

            var targetHeight = (await _readerFactory.Create(currentBlockHash, currentBlockHeight).GetParentChainHeight
                                   .CallAsync(new Empty())).Value + 1;
            while (i < length)
            {
                var parentChainBlockData =
                    _blockCacheEntityConsumer.Take<ParentChainBlockData>(parentChainId, targetHeight, false);
                if (parentChainBlockData == null)
                    throw new ValidateNextTimeBlockValidationException(
                        $"Parent chain data not found, chainId: {ChainHelper.ConvertChainIdToBase58(parentChainId)}, parent chain height: {targetHeight}.");
                
                if (!parentChainBlockDataList[i].Equals(parentChainBlockData))
                    return false;

                targetHeight++;
                i++;
            }

            return true;
        }

        public async Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight)
        {
            var crossChainBlockData = await _readerFactory.Create(currentBlockHash,
                    currentBlockHeight).GetIndexedCrossChainBlockDataByHeight
                .CallAsync(new SInt64Value() {Value = currentBlockHeight});
            return crossChainBlockData;
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

        public async Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId, Hash blockHash, long blockHeight)
        {
            return await _readerFactory.Create(blockHash, blockHeight).GetChainInitializationData.CallAsync(new SInt32Value()
            {
                Value = chainId
            });
        }

        public void UpdateWithLibIndex(BlockIndex blockIndex)
        {
            // clear useless cache
            var toRemoveList = _indexedCrossChainBlockData.Where(kv => kv.Value.PreviousBlockHeight < blockIndex.Height)
                .Select(kv => kv.Key).ToList();
            foreach (var hash in toRemoveList)
            {
                _indexedCrossChainBlockData.Remove(hash);
            }
        }
    }
}
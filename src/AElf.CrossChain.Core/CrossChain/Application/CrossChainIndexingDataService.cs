using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain
{
    internal class CrossChainIndexingDataService : ICrossChainIndexingDataService
    {
        private readonly IReaderFactory _readerFactory;
        private readonly IBlockCacheEntityConsumer _blockCacheEntityConsumer;
        private readonly IIndexedCrossChainBlockDataProvider _indexedCrossChainBlockDataProvider;
        private readonly IIrreversibleBlockStateProvider _irreversibleBlockStateProvider;

        public ILogger<CrossChainIndexingDataService> Logger { get; set; }

        public IOptionsSnapshot<CrossChainConfigOptions> CrossChainOptions { get; set; }
        
        public CrossChainIndexingDataService(IReaderFactory readerFactory, IBlockCacheEntityConsumer blockCacheEntityConsumer, 
            IIndexedCrossChainBlockDataProvider indexedCrossChainBlockDataProvider, IIrreversibleBlockStateProvider irreversibleBlockStateProvider)
        {
            _readerFactory = readerFactory;
            _blockCacheEntityConsumer = blockCacheEntityConsumer;
            _indexedCrossChainBlockDataProvider = indexedCrossChainBlockDataProvider;
            _irreversibleBlockStateProvider = irreversibleBlockStateProvider;
        }

        private async Task<List<SideChainBlockData>> GetNonIndexedSideChainBlockDataAsync(Hash blockHash, long blockHeight)
        {
            var sideChainBlockDataList = new List<SideChainBlockData>();
            var sideChainIndexingInformationList = await _readerFactory.Create(blockHash, blockHeight)
                .GetSideChainIndexingInformationList.CallAsync(new Empty());
            foreach (var sideChainIndexingInformation in sideChainIndexingInformationList.IndexingInformationList)
            {
                var libDto = await _irreversibleBlockStateProvider.GetLastIrreversibleBlockHashAndHeightAsync();
                var sideChainId = sideChainIndexingInformation.ChainId;
                var sideChainHeightInLibValue = await _readerFactory.Create(libDto.BlockHash, libDto.BlockHeight)
                    .GetSideChainHeight.CallAsync(new SInt32Value {Value = sideChainId});

                long toBeIndexedCount;
                long targetHeight;
                var sideChainHeightInLib = sideChainHeightInLibValue?.Value ?? 0;
                if (sideChainHeightInLib > 0)
                {
                    targetHeight = sideChainIndexingInformation.IndexedHeight + 1;
                    toBeIndexedCount = Math.Min(CrossChainOptions.Value.MaximalCountForIndexingSideChainBlock,
                        sideChainIndexingInformation.ToBeIndexedCount);
                    Logger.LogTrace(
                        $"Target height {targetHeight} of side chain " +
                        $"{ChainHelper.ConvertChainIdToBase58(sideChainId)}.");
                }
                else if (sideChainIndexingInformation.IndexedHeight > 0)
                {
                    toBeIndexedCount = 0;
                    targetHeight = sideChainIndexingInformation.IndexedHeight + 1;
                }
                else
                {
                    toBeIndexedCount = 1;
                    targetHeight = 1;
                    Logger.LogTrace(
                        $"Target height {targetHeight} of side chain " +
                        $"{ChainHelper.ConvertChainIdToBase58(sideChainId)}.");
                }

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
        
        private async Task<List<ParentChainBlockData>> GetNonIndexedParentChainBlockDataAsync(Hash blockHash, long blockHeight)
        {
            var parentChainBlockDataList = new List<ParentChainBlockData>();
            var returnValue = await _readerFactory.Create(blockHash, blockHeight).GetParentChainId
                .CallAsync(new Empty());
            var parentChainId = returnValue?.Value ?? 0;
            if (parentChainId == 0)
            {
                //Logger.LogTrace("No configured parent chain");
                // no configured parent chain
                return parentChainBlockDataList;
            }
                
            int length = CrossChainOptions.Value.MaximalCountForIndexingParentChainBlock;
            var heightInState = (await _readerFactory.Create(blockHash, blockHeight).GetParentChainHeight
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

        public async Task<bool> ValidateSideChainBlockDataAsync(List<SideChainBlockData> sideChainBlockDataList, 
            Hash blockHash, long blockHeight)
        {
            var sideChainValidatedHeightDict = new Dictionary<int, long>(); // chain id => validated height
            foreach (var sideChainBlockData in sideChainBlockDataList)
            {
                if (!sideChainValidatedHeightDict.TryGetValue(sideChainBlockData.ChainId, out var validatedHeight))
                {
                    var height = await _readerFactory.Create(blockHash, blockHeight).GetSideChainHeight
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
        
        public async Task<bool> ValidateParentChainBlockDataAsync(List<ParentChainBlockData> parentChainBlockDataList, 
            Hash blockHash, long blockHeight)
        {
            if (parentChainBlockDataList.Count == 0)
                return true;
            var parentChainId = (await _readerFactory.Create(blockHash, blockHeight).GetParentChainId
                .CallAsync(new Empty())).Value;
            if (parentChainId == 0)
                // no configured parent chain
                return false;

            var length = parentChainBlockDataList.Count;

            var i = 0;

            var targetHeight = (await _readerFactory.Create(blockHash, blockHeight).GetParentChainHeight
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

        public async Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash blockHash, long blockHeight)
        {
            var crossChainBlockData = await _readerFactory.Create(blockHash, blockHeight)
                .GetIndexedCrossChainBlockDataByHeight.CallAsync(new SInt64Value {Value = blockHeight});
            return crossChainBlockData;
        }
        
        public async Task<IndexedSideChainBlockData> GetIndexedSideChainBlockDataAsync(Hash blockHash, long blockHeight)
        {
            var indexedSideChainBlockData = await _readerFactory.Create(blockHash, blockHeight)
                .GetIndexedSideChainBlockDataByHeight.CallAsync(new SInt64Value {Value = blockHeight});
            return indexedSideChainBlockData;
        }

        /// <summary>
        /// This method returns cross chain data.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        public async Task<CrossChainBlockData> GetCrossChainBlockDataForNextMiningAsync(Hash blockHash, long blockHeight)
        {
            var sideChainBlockData = await GetNonIndexedSideChainBlockDataAsync(blockHash, blockHeight);
            var parentChainBlockData = await GetNonIndexedParentChainBlockDataAsync(blockHash, blockHeight);

            if (sideChainBlockData.Count == 0 && parentChainBlockData.Count == 0)
                return null;
            var crossChainBlockData = new CrossChainBlockData();
            crossChainBlockData.ParentChainBlockData.AddRange(parentChainBlockData);
            crossChainBlockData.SideChainBlockData.AddRange(sideChainBlockData);
            crossChainBlockData.PreviousBlockHeight = blockHeight;
            
            _indexedCrossChainBlockDataProvider.SetIndexedBlockData(blockHash, crossChainBlockData);
            return crossChainBlockData;
        }

        /// <summary>
        /// This method returns cross chain data already used before.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <param name="previousBlockHeight"></param>
        /// <returns></returns>
        public CrossChainBlockData GetUsedCrossChainBlockDataForLastMining(Hash blockHash, long previousBlockHeight)
        {
            return _indexedCrossChainBlockDataProvider.GetIndexedBlockData(blockHash);
        }

        public void UpdateCrossChainDataWithLib(Hash blockHash, long blockHeight)
        {
            // clear useless cache
            _indexedCrossChainBlockDataProvider.ClearExpiredCrossChainBlockData(blockHeight);
        }
    }
}
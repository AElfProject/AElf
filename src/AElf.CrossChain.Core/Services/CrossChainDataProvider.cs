using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.CrossChain.Cache;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.CrossChain
{
    internal class CrossChainDataProvider : ICrossChainDataProvider, ISingletonDependency
    {
        private readonly IReaderFactory _readerFactory;
        
        private readonly IBlockCacheEntityConsumer _blockCacheEntityConsumer;
        public ILogger<CrossChainDataProvider> Logger { get; set; }

        private readonly Dictionary<Hash, CrossChainBlockData> _indexedCrossChainBlockData =
            new Dictionary<Hash, CrossChainBlockData>();

        public CrossChainDataProvider(IReaderFactory readerFactory, IBlockCacheEntityConsumer blockCacheEntityConsumer)
        {
            _readerFactory = readerFactory;
            _blockCacheEntityConsumer = blockCacheEntityConsumer;
        }

        public async Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight)
        {
            var sideChainBlockDataList = new List<SideChainBlockData>();
            var dict = await _readerFactory.Create(currentBlockHash, currentBlockHeight).GetSideChainIdAndHeight.CallAsync(new Empty());
            foreach (var idHeight in dict.IdHeightDict)
            {
                var i = 0;
                var targetHeight = idHeight.Value + 1;
                while (i < CrossChainConstants.MaximalCountForIndexingSideChainBlock)
                {
                    var sideChainBlockData = _blockCacheEntityConsumer.Take<SideChainBlockData>(idHeight.Key, targetHeight, true);
                    if (sideChainBlockData == null)
                    {
                        // no more available parent chain block info
                        break;
                    }
                    
                    sideChainBlockDataList.Add(sideChainBlockData);
                    Logger.LogTrace(
                        $"Got height {sideChainBlockData.SideChainHeight} of side chain  {ChainHelpers.ConvertChainIdToBase58(idHeight.Key)}");
                    targetHeight++;
                    i++;
                }
            }

            Logger.LogTrace($"Side chain block data count {sideChainBlockDataList.Count}");
            return sideChainBlockDataList;
        }

        public async Task<bool> ValidateSideChainBlockDataAsync(List<SideChainBlockData> sideChainBlockDataList, 
            Hash currentBlockHash, long currentBlockHeight)
        {
            bool isExceedSizeLimit = sideChainBlockDataList.GroupBy(b => b.SideChainId).Select(g => g.Count())
                .Any(count => count > CrossChainConstants.MaximalCountForIndexingSideChainBlock);

            if (isExceedSizeLimit)
                return false;

            var sideChainValidatedHeightDict = new Dictionary<int, long>(); // chain id => validated height
            foreach (var sideChainBlockData in sideChainBlockDataList)
            {
                if (!sideChainValidatedHeightDict.TryGetValue(sideChainBlockData.SideChainId, out var validatedHeight))
                {
                    var height = await _readerFactory.Create(currentBlockHash, currentBlockHeight).GetSideChainHeight
                        .CallAsync(
                            new SInt32Value()
                            {
                                Value = sideChainBlockData.SideChainId
                            });
                    validatedHeight = height?.Value ?? 0;
                }

                long targetHeight = validatedHeight + 1; 

                if (targetHeight != sideChainBlockData.SideChainHeight)
                    // this should not happen if it is good data.
                    return false;

                var cachedSideChainBlockData = _blockCacheEntityConsumer.Take<SideChainBlockData>(sideChainBlockData.SideChainId, targetHeight, false);
                if (cachedSideChainBlockData == null)
                    throw new ValidateNextTimeBlockValidationException("Cross chain data is not ready.");
                if(!cachedSideChainBlockData.Equals(sideChainBlockData))
                    return false;
                sideChainValidatedHeightDict[sideChainBlockData.SideChainId] = sideChainBlockData.SideChainHeight;
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
                
            const int length = CrossChainConstants.MaximalCountForIndexingParentChainBlock;
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
                Logger.LogTrace($"Got parent chain height {parentChainBlockData.ParentChainHeight}");
                targetHeight++;
                i++;
            }
            
            Logger.LogTrace($"Parent chain block data count {parentChainBlockDataList.Count}");
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

            if (length > CrossChainConstants.MaximalCountForIndexingParentChainBlock)
                return false;

            var i = 0;

            var targetHeight = (await _readerFactory.Create(currentBlockHash, currentBlockHeight).GetParentChainHeight
                                   .CallAsync(new Empty())).Value + 1;
            var res = true;
            while (i < length)
            {
                var parentChainBlockData = _blockCacheEntityConsumer.Take<ParentChainBlockData>(parentChainId, targetHeight, false);
                if (parentChainBlockData == null)
                {
                    throw new ValidateNextTimeBlockValidationException("Cross chain data is not ready.");
                }
                    
                if (!parentChainBlockDataList[i].Equals(parentChainBlockData))
                    // cached parent chain block info is not compatible with provided.
                    return false;
                targetHeight++;
                i++;
            }

            return res;
        }

        public async Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight)
        {
            var crossChainBlockData = await _readerFactory.Create(currentBlockHash,
                    currentBlockHeight).GetIndexedCrossChainBlockDataByHeight
                .CallAsync(new SInt64Value() {Value = currentBlockHeight});
            if (crossChainBlockData == null) return null;
            return CrossChainBlockData.Parser.ParseFrom(crossChainBlockData.ToByteString());
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

        public async Task<ChainInitializationInformation> GetChainInitializationContextAsync(int chainId, Hash blockHash, long blockHeight)
        {
            return await _readerFactory.Create(blockHash, blockHeight).GetChainInitializationContext.CallAsync(new SInt32Value()
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
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
        
        private readonly ICrossChainDataConsumer _crossChainDataConsumer;
        public ILogger<CrossChainDataProvider> Logger { get; set; }

        private readonly Dictionary<Hash, CrossChainBlockData> _indexedCrossChainBlockData =
            new Dictionary<Hash, CrossChainBlockData>();

        public CrossChainDataProvider(IReaderFactory readerFactory, ICrossChainDataConsumer crossChainDataConsumer)
        {
            _readerFactory = readerFactory;
            _crossChainDataConsumer = crossChainDataConsumer;
        }

        public async Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight)
        {
            var sideChainBlockData = new List<SideChainBlockData>();
            var dict = await _readerFactory.Create(currentBlockHash, currentBlockHeight).GetSideChainIdAndHeight.CallAsync(new Empty());
            foreach (var idHeight in dict.IdHeightDict)
            {
                var i = 0;
                var targetHeight = idHeight.Value + 1;
                while (i < CrossChainConstants.MaximalCountForIndexingSideChainBlock)
                {
                    var blockInfo = _crossChainDataConsumer.Take<SideChainBlockData>(idHeight.Key, targetHeight, true);
                    if (blockInfo == null)
                    {
                        // no more available parent chain block info
                        break;
                    }
                    
                    sideChainBlockData.Add(blockInfo);
                    Logger.LogTrace(
                        $"Got height {blockInfo.SideChainHeight} of side chain  {ChainHelpers.ConvertChainIdToBase58(idHeight.Key)}");
                    targetHeight++;
                    i++;
                }
            }

            Logger.LogTrace($"Side chain block data count {sideChainBlockData.Count}");
            return sideChainBlockData;
        }

        public async Task<bool> ValidateSideChainBlockDataAsync(List<SideChainBlockData> sideChainBlockData, 
            Hash currentBlockHash, long currentBlockHeight)
        {
            bool isExceedSizeLimit = sideChainBlockData.GroupBy(b => b.SideChainId).Select(g => g.Count())
                .Any(count => count > CrossChainConstants.MaximalCountForIndexingSideChainBlock);

            if (isExceedSizeLimit)
                return false;

            var sideChainValidatedHeightDict = new Dictionary<int, long>(); // chain id => validated height
            foreach (var blockInfo in sideChainBlockData)
            {
                if (!sideChainValidatedHeightDict.TryGetValue(blockInfo.SideChainId, out var validatedHeight))
                {
                    var height = await _readerFactory.Create(currentBlockHash, currentBlockHeight).GetSideChainHeight
                        .CallAsync(
                            new SInt32Value()
                            {
                                Value = blockInfo.SideChainId
                            });
                    validatedHeight = height?.Value ?? 0;
                }

                long targetHeight = validatedHeight + 1; 

                if (targetHeight != blockInfo.SideChainHeight)
                    // this should not happen if it is good data.
                    return false;

                var cachedBlockInfo = _crossChainDataConsumer.Take<SideChainBlockData>(blockInfo.SideChainId, targetHeight, false);
                if (cachedBlockInfo == null)
                    throw new ValidateNextTimeBlockValidationException("Cross chain data is not ready.");
                if(!cachedBlockInfo.Equals(blockInfo))
                    return false;
                sideChainValidatedHeightDict[blockInfo.SideChainId] = blockInfo.SideChainHeight;
            }

            return true;
        }

        public async Task<List<ParentChainBlockData>> GetParentChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight)
        {
            var parentChainBlockData = new List<ParentChainBlockData>();
            var returnValue = await _readerFactory.Create(currentBlockHash, currentBlockHeight).GetParentChainId
                .CallAsync(new Empty());
            var parentChainId = returnValue?.Value ?? 0;
            if (parentChainId == 0)
            {
                //Logger.LogTrace("No configured parent chain");
                // no configured parent chain
                return parentChainBlockData;
            }
                
            const int length = CrossChainConstants.MaximalCountForIndexingParentChainBlock;
            var heightInState = (await _readerFactory.Create(currentBlockHash, currentBlockHeight).GetParentChainHeight
                .CallAsync(new Empty())).Value;
            
            var targetHeight = heightInState + 1;
            Logger.LogTrace($"Target height {targetHeight}");

            var i = 0;
            while (i < length)
            {
                var blockInfo = _crossChainDataConsumer.Take<ParentChainBlockData>(parentChainId, targetHeight, true);
                if (blockInfo == null)
                {
                    // no more available parent chain block info
                    break;
                }

                parentChainBlockData.Add(blockInfo);
                Logger.LogTrace($"Got parent chain height {blockInfo.ParentChainHeight}");
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
            var parentChainId = (await _readerFactory.Create(currentBlockHash, currentBlockHeight).GetParentChainId
                .CallAsync(new Empty())).Value;
            if (parentChainId == 0)
                // no configured parent chain
                return false;

            var length = parentChainBlockData.Count;

            if (length > CrossChainConstants.MaximalCountForIndexingParentChainBlock)
                return false;

            var i = 0;

            var targetHeight = (await _readerFactory.Create(currentBlockHash, currentBlockHeight).GetParentChainHeight
                                   .CallAsync(new Empty())).Value + 1;
            var res = true;
            while (i < length)
            {
                var blockInfo = _crossChainDataConsumer.Take<ParentChainBlockData>(parentChainId, targetHeight, false);
                if (blockInfo == null)
                {
                    throw new ValidateNextTimeBlockValidationException("Cross chain data is not ready.");
                }
                    
                if (!parentChainBlockData[i].Equals(blockInfo))
                    // cached parent chain block info is not compatible with provided.
                    return false;
                targetHeight++;
                i++;
            }

            return res;
        }

        public async Task<IMessage> GetIndexedCrossChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight)
        {
            var message = await _readerFactory.Create(currentBlockHash,
                    currentBlockHeight).GetIndexedCrossChainBlockDataByHeight
                .CallAsync(new SInt64Value() {Value = currentBlockHeight});
            if (message == null) return null;
            return CrossChainBlockData.Parser.ParseFrom(message.ToByteString()) ;
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

        public async Task<IMessage> GetChainInitializationContextAsync(int chainId, Hash blockHash, long blockHeight)
        {
            return await _readerFactory.Create(blockHash, blockHeight).GetChainInitializationContext.CallAsync(new SInt32Value()
            {
                Value = chainId
            });
        }

        public void HandleLibEvent(IrreversibleBlockDto irreversibleBlockDto)
        {
            // clear useless cache
            var toRemoveList = _indexedCrossChainBlockData.Where(kv => kv.Value.PreviousBlockHeight < irreversibleBlockDto.BlockHeight)
                .Select(kv => kv.Key).ToList();
            foreach (var hash in toRemoveList)
            {
                _indexedCrossChainBlockData.Remove(hash);
            }
        }
    }
}
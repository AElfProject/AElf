using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache.Application;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    internal class CrossChainIndexingDataValidationService : ICrossChainIndexingDataValidationService, ITransientDependency
    {
        public ILogger<CrossChainIndexingDataValidationService> Logger { get; set; }
        private readonly IReaderFactory _readerFactory;
        private readonly IBlockCacheEntityConsumer _blockCacheEntityConsumer;

        public CrossChainIndexingDataValidationService(IReaderFactory readerFactory, 
            IBlockCacheEntityConsumer blockCacheEntityConsumer)
        {
            _readerFactory = readerFactory;
            _blockCacheEntityConsumer = blockCacheEntityConsumer;
        }


        public async Task<bool> ValidateCrossChainIndexingData(CrossChainBlockData crossChainBlockData, Block block)
        {
            var sideChainBlockDataValidationResult =
                await ValidateSideChainBlockDataAsync(crossChainBlockData.SideChainBlockData, block.GetHash(),
                    block.Height);
            if (!sideChainBlockDataValidationResult)
                return false;

            var parentChainBlockDataValidationResult =
                await ValidateParentChainBlockDataAsync(crossChainBlockData.ParentChainBlockData, block.GetHash(),
                    block.Height);
            
            return parentChainBlockDataValidationResult;
        }
        
        
        private async Task<bool> ValidateSideChainBlockDataAsync(IEnumerable<SideChainBlockData> multiSideChainBlockData, 
            Hash blockHash, long blockHeight)
        {
            var sideChainValidatedHeightDict = new Dictionary<int, long>(); // chain id => validated height
            foreach (var sideChainBlockData in multiSideChainBlockData)
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

                var targetHeight = validatedHeight + 1; 

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

            foreach (var chainIdHeight in sideChainValidatedHeightDict)
            {
                Logger.LogTrace(
                    $"Validated height {chainIdHeight.Value} from  chain {ChainHelper.ConvertChainIdToBase58(chainIdHeight.Key)} ");
            }

            return true;
        }
        
        private async Task<bool> ValidateParentChainBlockDataAsync(IEnumerable<ParentChainBlockData> multiParentChainBlockData, 
            Hash blockHash, long blockHeight)
        {
            var parentChainBlockDataList = multiParentChainBlockData.ToList();
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
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain.Cache;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Domain;

namespace AElf.CrossChain
{
    public class CrossChainDataProvider : ICrossChainDataProvider
    {
        private readonly ICrossChainContractReader _crossChainContractReader;
        private readonly ICrossChainDataConsumer _crossChainDataConsumer;

        public CrossChainDataProvider(ICrossChainContractReader crossChainContractReader,
            ICrossChainDataConsumer crossChainDataConsumer)
        {
            _crossChainContractReader = crossChainContractReader;
            _crossChainDataConsumer = crossChainDataConsumer;
        }

        public async Task<bool> GetSideChainBlockDataAsync(IList<SideChainBlockData> sideChainBlockData,
            Hash previousBlockHash, long preBlockHeight, bool isValidation = false)
        {
            if (!isValidation)
            {
                // this happens before mining
                if (sideChainBlockData.Count > 0)
                    return false;
                var chainContext = GenerateChainContext(previousBlockHash, preBlockHeight);
                var dict = await _crossChainContractReader.GetSideChainIdAndHeightAsync(chainContext);
                foreach (var idHeight in dict)
                {
                    // index only one block from one side chain which could be changed later.
                    // cause take these data before mining, the target height of consumer == height + 1
                    var blockInfo = _crossChainDataConsumer.TryTake(idHeight.Key, idHeight.Value + 1, true);
                    if (blockInfo == null)
                        continue;

                    sideChainBlockData.Add((SideChainBlockData) blockInfo);
                }

                return sideChainBlockData.Count > 0;
            }

            foreach (var blockInfo in sideChainBlockData)
            {
                // this happens after block execution
                // cause take these data after block execution, the target height of consumer == height.
                // return 0 if side chain not exist.
                var chainContext = GenerateChainContext(previousBlockHash, preBlockHeight);
                var targetHeight = await _crossChainContractReader.GetSideChainCurrentHeightAsync(blockInfo.ChainId, chainContext);
                if (targetHeight != blockInfo.Height)
                    // this should not happen if it is good data.
                    return false;

                var cachedBlockInfo = _crossChainDataConsumer.TryTake(blockInfo.ChainId, targetHeight, false);
                if (cachedBlockInfo == null || !cachedBlockInfo.Equals(blockInfo))
                    return false;
            }

            return true;
        }

        public async Task<bool> GetParentChainBlockDataAsync(
            IList<ParentChainBlockData> parentChainBlockData, Hash previousBlockHash, long preBlockHeight,
            bool isValidation = false)
        {
            if (!isValidation && parentChainBlockData.Count > 0)
                return false;
            var chainContext = GenerateChainContext(previousBlockHash, preBlockHeight);
            var parentChainId = await _crossChainContractReader.GetParentChainIdAsync(chainContext);
            if (parentChainId == 0)
                // no configured parent chain
                return false;

            // Size of result is GlobalConfig.MaximalCountForIndexingParentChainBlock if it is mining process.
            if (isValidation && parentChainBlockData.Count > CrossChainConsts.MaximalCountForIndexingParentChainBlock)
                return false;

            int length = isValidation
                ? parentChainBlockData.Count
                : CrossChainConsts.MaximalCountForIndexingParentChainBlock;

            int i = 0;
            
            var heightInState =
                await _crossChainContractReader.GetParentChainCurrentHeightAsync(chainContext);
            long targetHeight = isValidation ? heightInState : heightInState + 1;
            var res = true;
            while (i < length)
            {
                var blockInfo = _crossChainDataConsumer.TryTake(parentChainId, targetHeight, !isValidation);
                if (blockInfo == null)
                {
                    // no more available parent chain block info
                    res = !isValidation;
                    break;
                }

                if (!isValidation)
                    parentChainBlockData.Add((ParentChainBlockData) blockInfo);
                else if (!parentChainBlockData[i].Equals(blockInfo))
                    // cached parent chain block info is not compatible with provided.
                    return false;
                targetHeight++;
                i++;
            }

            return res;
        }

        public async Task<bool> ActivateCrossChainCacheAsync(Hash blockHash, long blockHeight)
        {
            if (_crossChainDataConsumer.GetCachedChainCount() > 0)
                // caching layer already initialized
                return false;
            var chainContext = GenerateChainContext(blockHash, blockHeight);
            var dict = await _crossChainContractReader.GetAllChainsIdAndHeightAsync(chainContext);
            foreach (var idHeight in dict)
            {
                _crossChainDataConsumer.RegisterNewChainCache(idHeight.Key, idHeight.Value);
            }

            return true;
        }

        public void RegisterNewChain(int chainId)
        {
            _crossChainDataConsumer.RegisterNewChainCache(chainId, 0);
        }

        public async Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash previousBlockHash, long previousBlockHeight)
        {
            var chainContext = GenerateChainContext(previousBlockHash, previousBlockHeight);
            return await _crossChainContractReader.GetCrossChainBlockDataAsync(chainContext);
        }

        private IChainContext GenerateChainContext(Hash blockHash, long blockHeight)
        {
            var chainContext = new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            };
            return chainContext;
        }
        
    }
}
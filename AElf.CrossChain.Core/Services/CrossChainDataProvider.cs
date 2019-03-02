using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Domain;

namespace AElf.CrossChain
{
    public class CrossChainDataProvider : ICrossChainDataProvider
    {
        private readonly ICrossChainContractReader _crossChainContractReader;
        private readonly ICrossChainDataConsumer _crossChainDataConsumer;
        private readonly IChainManager _chainManager;

        public CrossChainDataProvider(ICrossChainContractReader crossChainContractReader,
            ICrossChainDataConsumer crossChainDataConsumer, IChainManager chainManager)
        {
            _crossChainContractReader = crossChainContractReader;
            _crossChainDataConsumer = crossChainDataConsumer;
            _chainManager = chainManager;
        }

        public async Task<bool> GetSideChainBlockDataAsync(IList<SideChainBlockData> sideChainBlockData,
            Hash previousBlockHash, ulong preBlockHeight, bool isValidation = false)
        {
            if (!isValidation)
            {
                // this happens before mining
                if (sideChainBlockData.Count > 0)
                    return false;
                var dict = await _crossChainContractReader.GetSideChainIdAndHeightAsync(previousBlockHash,
                    preBlockHeight);
                foreach (var keyValuePair in dict)
                {
                    // index only one block from one side chain which could be changed later.
                    // cause take these data before mining, the target height of consumer == height + 1
                    var blockInfo = _crossChainDataConsumer.TryTake(keyValuePair.Key, keyValuePair.Value + 1, true);
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
                var targetHeight = await _crossChainContractReader.GetSideChainCurrentHeightAsync(
                    blockInfo.ChainId,
                    previousBlockHash, preBlockHeight);
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
            IList<ParentChainBlockData> parentChainBlockData, Hash previousBlockHash, ulong preBlockHeight,
            bool isValidation = false)
        {
            if (!isValidation && parentChainBlockData.Count > 0)
                return false;
            var parent = await _crossChainContractReader.GetParentChainIdAsync(previousBlockHash, preBlockHeight);
            if (parent == 0)
                // no configured parent chain
                return false;

            // Size of result is GlobalConfig.MaximalCountForIndexingParentChainBlock if it is mining process.
            if (isValidation && parentChainBlockData.Count > CrossChainConsts.MaximalCountForIndexingParentChainBlock)
                return false;

            int length = isValidation
                ? parentChainBlockData.Count
                : CrossChainConsts.MaximalCountForIndexingParentChainBlock;

            int i = 0;
            ulong targetHeight =
                await _crossChainContractReader.GetParentChainCurrentHeightAsync(previousBlockHash, preBlockHeight);
            var res = true;
            while (i < length)
            {
                var blockInfo = _crossChainDataConsumer.TryTake(parent, targetHeight, !isValidation);
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

        public async Task<bool> ActivateCrossChainCacheAsync(Hash blockHash, ulong blockHeight)
        {
            if (_crossChainDataConsumer.GetCachedChainCount() > 0)
                // caching layer already initialized
                return false;
            var dict = await _crossChainContractReader.GetAllChainsIdAndHeightAsync(blockHash, blockHeight);
            foreach (var idHeight in dict)
            {
                _crossChainDataConsumer.RegisterNewChainCache(idHeight.Key, idHeight.Value);
            }

            return true;
        }

        public void RegisterNewChain()
        {
            _crossChainDataConsumer.RegisterNewChainCache(_chainManager.GetChainId(), 0);
        }
    }
}
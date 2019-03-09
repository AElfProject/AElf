using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain.Cache;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Domain;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class CrossChainDataProvider : ICrossChainDataProvider, ITransientDependency
    {
        private readonly ICrossChainContractReader _crossChainContractReader;
        private readonly ICrossChainDataConsumer _crossChainDataConsumer;

        public CrossChainDataProvider(ICrossChainContractReader crossChainContractReader,
            ICrossChainDataConsumer crossChainDataConsumer)
        {
            _crossChainContractReader = crossChainContractReader;
            _crossChainDataConsumer = crossChainDataConsumer;
        }

        public async Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(Hash previousBlockHash, long preBlockHeight)
        {
            var sideChainBlockData = new List<SideChainBlockData>(); 
            var dict = await _crossChainContractReader.GetSideChainIdAndHeightAsync(previousBlockHash,
                preBlockHeight);
            foreach (var idHeight in dict)
            {
                // index only one block from one side chain which could be changed later.
                // cause take these data before mining, the target height of consumer == height + 1
                var blockInfo = _crossChainDataConsumer.TryTake(idHeight.Key, idHeight.Value + 1, true);
                if (blockInfo == null)
                    continue;

                sideChainBlockData.Add((SideChainBlockData) blockInfo);
            }

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
                        preBlockHeight);
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
                // no configured parent chain
                return parentChainBlockData;
            const int length = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            var heightInState =
                await _crossChainContractReader.GetParentChainCurrentHeightAsync(previousBlockHash, preBlockHeight);
            
            var targetHeight = heightInState + 1;
            
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
                targetHeight++;
                i++;
            }

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
                await _crossChainContractReader.GetParentChainCurrentHeightAsync(previousBlockHash, preBlockHeight);
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
//            if (_crossChainDataConsumer.GetCachedChainCount() > 0)
//                // caching layer already initialized
//                return false;
            var dict = await _crossChainContractReader.GetAllChainsIdAndHeightAsync(blockHash, blockHeight);
            foreach (var chainIdHeight in dict)
            {
                if(!_crossChainDataConsumer.CheckAlreadyCachedChain(chainIdHeight.Key))
                    _crossChainDataConsumer.RegisterNewChainCache(chainIdHeight.Key, chainIdHeight.Value);
            }
        }

        public void RegisterNewChain(int chainId)
        {
            _crossChainDataConsumer.RegisterNewChainCache(chainId);
        }

        public async Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash previousBlockHash, long previousBlockHeight)
        {
            return await _crossChainContractReader.GetIndexedCrossChainBlockDataAsync(previousBlockHash, previousBlockHeight);
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration.Config.GRPC;
using AElf.Crosschain.Grpc.Client;
using AElf.Kernel;
using AElf.Kernel.Storages;
using Akka.Util.Internal;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;

namespace AElf.Crosschain.Grpc
{
    public class GrpcCrossChainDataProvider : ICrossChainDataProvider
    {
        private readonly Dictionary<int, BlockInfoCache> _grpcSideChainClients =
            new Dictionary<int, BlockInfoCache>();

        internal BlockInfoCache ParentChainBlockInfoCache {get; set;}
        private readonly IChainHeightStore _chainHeightStore;
        public GrpcCrossChainDataProvider(IChainHeightStore chainHeightStore)
        {
            _chainHeightStore = chainHeightStore;
            // Event listener for new side chain block info cache created.
        }

        public async Task<bool> GetSideChainBlockInfo(List<SideChainBlockInfo> sideChainBlockInfo)
        {
            if (sideChainBlockInfo.Count == 0)
            {
                foreach (var _ in _grpcSideChainClients)
                {
                    // take side chain info
                    // index only one block from one side chain.
                    // this could be changed later.
                    var targetHeight = await GetChainTargetHeight(_.Key);
                    if (!_.Value.TryTake(targetHeight, out var blockInfo, true))
                        continue;

                    sideChainBlockInfo.Append((SideChainBlockInfo) blockInfo);
                }
            }
            else
            {
                foreach (var blockInfo in sideChainBlockInfo)
                {
                    if (!_grpcSideChainClients.TryGetValue(blockInfo.ChainId, out var cache))
                        // TODO: this could be changed.
                        return true;
                    var targetHeight = await GetChainTargetHeight(blockInfo.ChainId);

                    sideChainBlockInfo.Append(blockInfo);
                    if (!cache.TryTake(targetHeight, out var cachedBlockInfo) || !blockInfo.Equals(cachedBlockInfo))
                        return false;
                }
            }
            return sideChainBlockInfo.Count > 0;
        }

        public async Task<bool> GetParentChainBlockInfo(List<ParentChainBlockInfo> parentChainBlockInfo)
        {
            var chainId = ParentChainBlockInfoCache?.ChainId ?? 0;
            if (chainId == 0)
                // no configured parent chain
                return false;
            
            ulong targetHeight = await GetChainTargetHeight(chainId);
            if (parentChainBlockInfo.Count == 0)
            {
                if (ParentChainBlockInfoCache == null)
                    return false;
            }
            var isMining = parentChainBlockInfo.Count == 0;
            // Size of result is GlobalConfig.MaximalCountForIndexingParentChainBlock if it is mining process.
            if (!isMining && parentChainBlockInfo.Count > GlobalConfig.MaximalCountForIndexingParentChainBlock)
                return false;
            int length = isMining ? GlobalConfig.MaximalCountForIndexingParentChainBlock : parentChainBlockInfo.Count;
            
            int i = 0;
            while (i++ < length)
            {
                if (!ParentChainBlockInfoCache.TryTake(targetHeight, out var blockInfo, isMining))
                {
                    // no more available parent chain block info
                    return isMining;
                }
                
                if(isMining)
                    parentChainBlockInfo.Add((ParentChainBlockInfo) blockInfo);
                else if (!parentChainBlockInfo[i].Equals(blockInfo))
                    // cached parent chain block info is not compatible with provided.
                    return false;
                targetHeight++;
            }
            
            return parentChainBlockInfo.Count > 0;
        }

        public bool AddNewSideChainCache(BlockInfoCache blockInfoCache)
        {
            return _grpcSideChainClients.TryAdd(blockInfoCache.ChainId, blockInfoCache);
        }

        private async Task<ulong> GetChainTargetHeight(int chainId)
        {
            var height = await _chainHeightStore.GetAsync<UInt64Value>(chainId.ToHex());
            return height?.Value + 1 ??  GlobalConfig.GenesisBlockHeight;
        }

    }
}
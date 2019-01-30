using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Crosschain.Grpc.Client;
using AElf.Kernel;
using AElf.Kernel.Storages;

namespace AElf.Crosschain.Grpc
{
    public class GrpcCrossChainDataProvider : ICrossChainDataProvider
    {
        private readonly Dictionary<int, BlockInfoCache> _grpcSideChainClients =
            new Dictionary<int, BlockInfoCache>();
        
        private readonly ICrossChainContractReader _crossChainInfoReader;
        private readonly IChainHeightStore _chainHeightStore;
        public GrpcCrossChainDataProvider(IChainHeightStore chainHeightStore)
        {
            _chainHeightStore = chainHeightStore;
            // subscribe new cross chain data event here
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
                    var targetHeight = await _chainHeightStore.GetAsync<ulong>(_.Key);
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
                    var targetHeight = await GetSideChainTargetHeight(blockInfo.ChainId);

                    sideChainBlockInfo.Append(blockInfo);
                    return cache.TryTake(targetHeight, out var cachedBlockInfo) &&
                           blockInfo.Equals(cachedBlockInfo);
                }
            }

            return true;
        }

        public bool GetParentChainBlockInfo(ref ParentChainBlockInfo[] parentChainBlockInfo)
        {
            throw new System.NotImplementedException();
        }
        
        private async Task<ulong> GetSideChainTargetHeight(int chainId)
        {
            var height = await _crossChainInfoReader.GetSideChainCurrentHeightAsync(chainId);
            return height == 0 ? GlobalConfig.GenesisBlockHeight : height + 1;
        }
    }
}
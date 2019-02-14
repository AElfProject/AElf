using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Storages;
using Akka.Util.Internal;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Crosschain.Grpc
{
    public class GrpcCrossChainDataProvider : ICrossChainDataProvider
    {
        private readonly Dictionary<int, BlockInfoCache> _grpcSideChainClients =
            new Dictionary<int, BlockInfoCache>();

        internal BlockInfoCache ParentChainBlockInfoCache {get; set;}
        private readonly IChainHeightStore _chainHeightStore;
        private delegate void NewSideChainHandler(IClientBase clientBase);

        private readonly NewSideChainHandler _newSideChainHandler;
        public ILocalEventBus LocalEventBus { get; set; }
        public GrpcCrossChainDataProvider(IChainHeightStore chainHeightStore, IClientService clientService)
        {
            _chainHeightStore = chainHeightStore;
            LocalEventBus = NullLocalEventBus.Instance;
            _newSideChainHandler += clientService.CreateClient;
            _newSideChainHandler += AddNewSideChainCache;
            LocalEventBus.Subscribe<NewSideChainConnectionReceivedEvent>(OnNewSideChainConnectionReceivedEvent);
        }

        public async Task<bool> GetSideChainBlockData(IList<SideChainBlockData> sideChainBlockData)
        {
            if (sideChainBlockData.Count == 0)
            {
                foreach (var _ in _grpcSideChainClients)
                {
                    // take side chain info
                    // index only one block from one side chain.
                    // this could be changed later.
                    var targetHeight = await GetChainTargetHeight(_.Key);
                    if (!_.Value.TryTake(targetHeight, out var blockInfo, true))
                        continue;

                    sideChainBlockData.Append((SideChainBlockData) blockInfo);
                }
            }
            else
            {
                foreach (var blockInfo in sideChainBlockData)
                {
                    if (!_grpcSideChainClients.TryGetValue(blockInfo.ChainId, out var cache))
                        // TODO: this could be changed.
                        return true;
                    var targetHeight = await GetChainTargetHeight(blockInfo.ChainId);

                    sideChainBlockData.Append(blockInfo);
                    if (!cache.TryTake(targetHeight, out var cachedBlockInfo) || !blockInfo.Equals(cachedBlockInfo))
                        return false;
                }
            }
            return sideChainBlockData.Count > 0;
        }

        public async Task<bool> GetParentChainBlockData(IList<ParentChainBlockData> parentChainBlockData)
        {
            var chainId = ParentChainBlockInfoCache?.ChainId ?? 0;
            if (chainId == 0)
                // no configured parent chain
                return false;
            
            ulong targetHeight = await GetChainTargetHeight(chainId);
            if (parentChainBlockData.Count == 0)
            {
                if (ParentChainBlockInfoCache == null)
                    return false;
            }
            var isMining = parentChainBlockData.Count == 0;
            // Size of result is GlobalConfig.MaximalCountForIndexingParentChainBlock if it is mining process.
            if (!isMining && parentChainBlockData.Count > GlobalConfig.MaximalCountForIndexingParentChainBlock)
                return false;
            int length = isMining ? GlobalConfig.MaximalCountForIndexingParentChainBlock : parentChainBlockData.Count;
            
            int i = 0;
            while (i++ < length)
            {
                if (!ParentChainBlockInfoCache.TryTake(targetHeight, out var blockInfo, isMining))
                {
                    // no more available parent chain block info
                    return isMining;
                }
                
                if(isMining)
                    parentChainBlockData.Add((ParentChainBlockData) blockInfo);
                else if (!parentChainBlockData[i].Equals(blockInfo))
                    // cached parent chain block info is not compatible with provided.
                    return false;
                targetHeight++;
            }
            
            return parentChainBlockData.Count > 0;
        }

        public void AddNewSideChainCache(IClientBase clientBase)
        {
            _grpcSideChainClients.TryAdd(clientBase.BlockInfoCache.ChainId, clientBase.BlockInfoCache);
        }

        private async Task<ulong> GetChainTargetHeight(int chainId)
        {
            var height = await _chainHeightStore.GetAsync<UInt64Value>(chainId.ToStorageKey());
            return height?.Value + 1 ??  GlobalConfig.GenesisBlockHeight;
        }

        private Task OnNewSideChainConnectionReceivedEvent(NewSideChainConnectionReceivedEvent @event)
        {
            _newSideChainHandler(@event.ClientBase);
            return Task.CompletedTask;
        }
    }
}
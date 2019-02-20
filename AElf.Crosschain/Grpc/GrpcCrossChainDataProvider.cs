using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Volo.Abp.EventBus.Local;

namespace AElf.Crosschain.Grpc
{
    public class GrpcCrossChainDataProvider : ICrossChainDataProvider
    {
        private readonly Dictionary<int, BlockInfoCache> _grpcSideChainClients =
            new Dictionary<int, BlockInfoCache>();

        internal BlockInfoCache ParentChainBlockInfoCache {get; set;}

        private delegate void NewSideChainHandler(IClientBase clientBase);

        private readonly NewSideChainHandler _newSideChainHandler;
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
        public ILocalEventBus LocalEventBus { get; set; }
        public GrpcCrossChainDataProvider(IClientService clientService, 
            ISmartContractExecutiveService smartContractExecutiveService)
        {
            _smartContractExecutiveService = smartContractExecutiveService;
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
                    var targetHeight = await GetSideChainTargetHeight(_.Key);
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
                    var targetHeight = await GetSideChainTargetHeight(blockInfo.ChainId);

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
            
            ulong targetHeight = await GetParentChainTargetHeight(chainId);
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
            _grpcSideChainClients.Add(clientBase.BlockInfoCache.ChainId, clientBase.BlockInfoCache);
        }

        private async Task<ulong> GetSideChainTargetHeight(int chainId)
        {
            var crossChainContractMethodAddress = ContractHelpers.GetCrossChainContractAddress(chainId);
            return await GenerateReadOnlyTransactionForChainHeight(chainId, crossChainContractMethodAddress, 
                CrossChainConsts.GetSideChainHeightMthodName, chainId.DumpBase58());
        }

        private async Task<ulong> GetParentChainTargetHeight(int chainId)
        {
            var crossChainContractMethodAddress = ContractHelpers.GetCrossChainContractAddress(chainId);
            return await GenerateReadOnlyTransactionForChainHeight(chainId, crossChainContractMethodAddress, 
                CrossChainConsts.GetParentChainHeightMethodName);
        }

        private async Task<ulong> GenerateReadOnlyTransactionForChainHeight(int chainId, Address toAddress, string methodName, params object[] @params)
        {
            var transaction =  new Transaction
            {
                To = toAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(@params)),
            };
            var trace = new TransactionTrace()
            {
                TransactionId = transaction.GetHash()
            };
            var txCtxt = new TransactionContext
            {
                Transaction = transaction,
                Trace = trace
            };
            var executive =
                await _smartContractExecutiveService.GetExecutiveAsync(chainId, transaction.To);
            await executive.SetTransactionContext(txCtxt).Apply();
            ulong height = (ulong) trace.RetVal.Data.DeserializeToType(typeof(ulong));
            return height == 0 ? height + 1 : GlobalConfig.GenesisBlockHeight;
        }

        private Task OnNewSideChainConnectionReceivedEvent(NewSideChainConnectionReceivedEvent @event)
        {
            _newSideChainHandler(@event.ClientBase);
            return Task.CompletedTask;
        }
    }
}
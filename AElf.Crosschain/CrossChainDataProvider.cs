using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Crosschain.EventMessage;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Crosschain
{
    public class CrossChainDataProvider : ICrossChainDataProvider, 
        ILocalEventHandler<NewSideChainConnectionReceivedEvent>, ILocalEventHandler<NewParentChainConnectionEvent>
    {
        private readonly Dictionary<int, BlockInfoCache> _sideChainClients =
            new Dictionary<int, BlockInfoCache>();

        internal BlockInfoCache ParentChainBlockInfoCache {get; set;}

        private delegate void NewSideChainHandler(IClientBase clientBase);
        private readonly NewSideChainHandler _newSideChainHandler;
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
        private readonly IAccountService _accountService;
        //public ILocalEventBus LocalEventBus { get; set; }
        public CrossChainDataProvider(IClientService clientService, 
            ISmartContractExecutiveService smartContractExecutiveService, IAccountService accountService)
        {
            _smartContractExecutiveService = smartContractExecutiveService;
            //LocalEventBus = NullLocalEventBus.Instance;
            _newSideChainHandler += clientService.CreateClient;
            _newSideChainHandler += AddNewSideChainCache;
            _accountService = accountService;
            //LocalEventBus.Subscribe<NewSideChainConnectionReceivedEvent>(OnNewSideChainConnectionReceivedEvent);
        }

        public async Task<bool> GetSideChainBlockData(IList<SideChainBlockData> sideChainBlockData, bool isValidation = false)
        {
            if (!isValidation )
            {
                if (sideChainBlockData.Count > 0)
                    return false;
                foreach (var _ in _sideChainClients)
                {
                    // take side chain info
                    // index only one block from one side chain.
                    // this could be changed later.
                    var targetHeight = await GetSideChainTargetHeight(_.Key);
                    if (!_.Value.TryTake(targetHeight, out var blockInfo, true))
                        continue;

                    sideChainBlockData.Add((SideChainBlockData) blockInfo);
                }
                return sideChainBlockData.Count > 0;
            }
            foreach (var blockInfo in sideChainBlockData)
            {
                if (!_sideChainClients.TryGetValue(blockInfo.ChainId, out var cache))
                    // TODO: this could be changed.
                    return false;
                var targetHeight = await GetSideChainTargetHeight(blockInfo.ChainId);

                if (targetHeight != blockInfo.Height 
                    || !cache.TryTake(targetHeight, out var cachedBlockInfo, false) 
                    || !blockInfo.Equals(cachedBlockInfo))
                    return false;
            }
            return true;
        }

        public async Task<bool> GetParentChainBlockData(IList<ParentChainBlockData> parentChainBlockData, bool isValidation = false)
        {
            if (!isValidation && parentChainBlockData.Count > 0)
                return false;
            var chainId = ParentChainBlockInfoCache?.ChainId ?? 0;
            if (chainId == 0)
                // no configured parent chain
                return false;
            
            // Size of result is GlobalConfig.MaximalCountForIndexingParentChainBlock if it is mining process.
            if (isValidation && parentChainBlockData.Count > CrossChainConsts.MaximalCountForIndexingParentChainBlock)
                return false;
            
            int length = isValidation ? parentChainBlockData.Count : CrossChainConsts.MaximalCountForIndexingParentChainBlock ;
            
            int i = 0;
            ulong targetHeight = await GetParentChainTargetHeight(chainId);
            var res = true;
            while (i < length)
            {
                if (!ParentChainBlockInfoCache.TryTake(targetHeight, out var blockInfo, !isValidation))
                {
                    // no more available parent chain block info
                    res = !isValidation;
                    break;
                }
                
                if(!isValidation)
                    parentChainBlockData.Add((ParentChainBlockData) blockInfo);
                else if (!parentChainBlockData[i].Equals(blockInfo))
                    // cached parent chain block info is not compatible with provided.
                    return false;
                targetHeight++;
                i++;
            }
            
            return res;
        }

        public void AddNewSideChainCache(IClientBase clientBase)
        {
            _sideChainClients.Add(clientBase.BlockInfoCache.ChainId, clientBase.BlockInfoCache);
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
                From = await _accountService.GetAccountAsync(),
                To = toAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(@params)),
            };
            var trace = new TransactionTrace()
            {
                TransactionId = transaction.GetHash(),
                RetVal = new RetVal()
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

//        public int GetCachedBlockDataCount(int chainId)
//        {
//            if(_sideChainClients.TryGetValue(chainId, out var blockInfoCache))
//                return blockInfoCache;
//        }

        public int GetCachedChainCount()
        {
            return _sideChainClients.Count;
        }

        public Task HandleEventAsync(NewSideChainConnectionReceivedEvent eventData)
        {
            _newSideChainHandler(eventData.ClientBase);
            return Task.CompletedTask;
        }

        public Task HandleEventAsync(NewParentChainConnectionEvent eventData)
        {
            ParentChainBlockInfoCache = eventData.ClientBase.BlockInfoCache;
            return Task.CompletedTask;
        }
    }
}
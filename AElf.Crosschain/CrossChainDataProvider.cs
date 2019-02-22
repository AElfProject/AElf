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
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.Crosschain
{
    public class CrossChainDataProvider : ICrossChainDataProvider, 
        ILocalEventHandler<NewSideChainEvent>, ILocalEventHandler<NewParentChainEvent>
    {
        private readonly Dictionary<int, ICrossChainDataConsumer> _sideChainBlockDataConsumers =
            new Dictionary<int, ICrossChainDataConsumer>();

        internal ICrossChainDataConsumer ParentChainBlockDataConsumer {get; set;}

//        private delegate void NewSideChainDataProducerCreationHandler(ICrossChainDataProducer crossChainDataProducer);
//        private readonly NewSideChainDataProducerCreationHandler _newSideChainDataProducerCreationHandler;
//        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
//        private readonly IAccountService _accountService;

        private readonly ICrossChainDataProducerConsumerService _crossChainDataProducerConsumerService;
        private readonly ICrossChainContractReader _crossChainContractReader;
        private readonly ChainOptions _chainOptions;
        //public ILocalEventBus LocalEventBus { get; set; }
        public CrossChainDataProvider(ICrossChainContractReader crossChainContractReader, 
            ICrossChainDataProducerConsumerService crossChainDataProducerConsumerService, IOptionsSnapshot<ChainOptions> options)
        {
            _crossChainContractReader = crossChainContractReader;
            _crossChainDataProducerConsumerService = crossChainDataProducerConsumerService;
            _chainOptions = options.Value;
            //LocalEventBus = NullLocalEventBus.Instance;
            //_newSideChainDataProducerCreationHandler += clientService.CreateConsumerProducer;
            //LocalEventBus.Subscribe<NewSideChainEvent>(OnNewSideChainConnectionReceivedEvent);
        }

        public async Task<bool> GetSideChainBlockDataAsync(int chainId, IList<SideChainBlockData> sideChainBlockData, bool isValidation = false)
        {
            if (!isValidation)
            {
                if (sideChainBlockData.Count > 0)
                    return false;
                foreach (var _ in _sideChainBlockDataConsumers)
                {
                    // take side chain info
                    // index only one block from one side chain.
                    // this could be changed later.
                    var targetHeight = await _crossChainContractReader.GetSideChainCurrentHeightAsync(chainId, _.Key);
                    var blockInfo = _.Value.TryTake(targetHeight, true);
                    if (blockInfo == null)
                        continue;

                    sideChainBlockData.Add((SideChainBlockData) blockInfo);
                }
                return sideChainBlockData.Count > 0;
            }
            foreach (var blockInfo in sideChainBlockData)
            {
                if (!_sideChainBlockDataConsumers.TryGetValue(blockInfo.ChainId, out var crossChainDataConsumer))
                    // TODO: this could be changed.
                    return false;

                var targetHeight = await _crossChainContractReader.GetSideChainCurrentHeightAsync(chainId, blockInfo.ChainId);
                if (targetHeight != blockInfo.Height)
                    return false;
                
                var cachedBlockInfo = crossChainDataConsumer.TryTake(targetHeight, false);
                if (cachedBlockInfo == null || !cachedBlockInfo.Equals(blockInfo))
                    return false;
            }
            return true;
        }

        public async Task<bool> GetParentChainBlockDataAsync(int chainId, IList<ParentChainBlockData> parentChainBlockData, bool isValidation = false)
        {
            if (!isValidation && parentChainBlockData.Count > 0 || ParentChainBlockDataConsumer == null)
                return false;
            var parentChainId = ParentChainBlockDataConsumer.ChainId;
            if (parentChainId == 0)
                // no configured parent chain
                return false;
            
            // Size of result is GlobalConfig.MaximalCountForIndexingParentChainBlock if it is mining process.
            if (isValidation && parentChainBlockData.Count > CrossChainConsts.MaximalCountForIndexingParentChainBlock)
                return false;
            
            int length = isValidation ? parentChainBlockData.Count : CrossChainConsts.MaximalCountForIndexingParentChainBlock ;
            
            int i = 0;
            ulong targetHeight = await _crossChainContractReader.GetParentChainCurrentHeightAsync(chainId, parentChainId);
            var res = true;
            while (i < length)
            {
                var blockInfo = ParentChainBlockDataConsumer.TryTake(targetHeight, !isValidation);
                if (blockInfo == null)
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

//        private async Task<ulong> GetSideChainTargetHeight(int chainId)
//        {
//            var crossChainContractMethodAddress = ContractHelpers.GetCrossChainContractAddress(chainId);
//            return await GenerateReadOnlyTransactionForChainHeight(chainId, crossChainContractMethodAddress, 
//                CrossChainConsts.GetSideChainHeightMthodName, ChainHelpers.ConvertChainIdToBase58(chainId));
//        }
//
//        private async Task<ulong> GetParentChainTargetHeight(int chainId)
//        {
//            var crossChainContractMethodAddress = ContractHelpers.GetCrossChainContractAddress(chainId);
//            return await GenerateReadOnlyTransactionForChainHeight(chainId, crossChainContractMethodAddress, 
//                CrossChainConsts.GetParentChainHeightMethodName);
//        }

//        private async Task<ulong> GenerateReadOnlyTransactionForChainHeight(int chainId, Address toAddress, string methodName, params object[] @params)
//        {
//            var transaction =  new Transaction
//            {
//                From = await _accountService.GetAccountAsync(),
//                To = toAddress,
//                MethodName = methodName,
//                Params = ByteString.CopyFrom(ParamsPacker.Pack(@params)),
//            };
//            var trace = new TransactionTrace()
//            {
//                TransactionId = transaction.GetHash(),
//                RetVal = new RetVal()
//            };
//            var txCtxt = new TransactionContext
//            {
//                Transaction = transaction,
//                Trace = trace
//            };
//            var executive =
//                await _smartContractExecutiveService.GetExecutiveAsync(chainId, transaction.To);
//            await executive.SetTransactionContext(txCtxt).Apply();
//            ulong height = (ulong) trace.RetVal.Data.DeserializeToType(typeof(ulong));
//            return height == 0 ? height + 1 : GlobalConfig.GenesisBlockHeight;
//        }

//        private Task OnNewSideChainConnectionReceivedEvent(NewSideChainEvent @event)
//        {
//            _newSideChainDataProducerCreationHandler(@event.CrossChainDataProducer);
//            return Task.CompletedTask;
//        }

//        public int GetCachedBlockDataCount(int chainId)
//        {
//            if(_sideChainBlockDataConsumers.TryGetValue(chainId, out var blockInfoCache))
//                return blockInfoCache;
//        }

        public int GetCachedChainCount()
        {
            return _sideChainBlockDataConsumers.Count;
        }

        public Task HandleEventAsync(NewSideChainEvent eventData)
        {
            var blockInfoCache = new BlockInfoCache();
            var dto = new CommunicationContextDto
            {
                CrossChainCommunicationContext = eventData.CrossChainCommunicationContext,
                BlockInfoCache = blockInfoCache,
                IsSideChain = true,
                TargetHeight = await _crossChainContractReader.GetSideChainCurrentHeightAsync(eventData.CrossChainCommunicationContext.ChainId)
            };
            var consumer, _ = _crossChainDataProducerConsumerService.CreateConsumerProducer();
            _sideChainBlockDataConsumers.Add(eventData.CrossChainDataProducer.ChainId, consumer);
            //_newSideChainDataProducerCreationHandler(eventData.CrossChainDataProducer);
            return Task.CompletedTask;
        }

        public Task HandleEventAsync(NewParentChainEvent eventData)
        {
            ParentChainBlockDataConsumer = eventData.CrossChainDataConsumer;
            return Task.CompletedTask;
        }
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.CrossChain
{
    public class CrossChainDataProvider : ICrossChainDataProvider
    {
        //private readonly Dictionary<int, ICrossChainDataConsumer> _sideChainBlockDataConsumers = new Dictionary<int, ICrossChainDataConsumer>();

        //internal ICrossChainDataConsumer ParentChainBlockDataConsumer {get; set;}

//        private delegate void NewSideChainDataProducerCreationHandler(ICrossChainDataProducer crossChainDataProducer);
//        private readonly NewSideChainDataProducerCreationHandler _newSideChainDataProducerCreationHandler;
//        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
//        private readonly IAccountService _accountService;

        //private readonly IProducerConsumerService _producerConsumerService;
        private readonly ICrossChainContractReader _crossChainContractReader;
        private readonly ICrossChainDataConsumer _crossChainDataConsumer;
        //private readonly IMultiChainBlockInfoCache _multiChainBlockInfoCache;
        //private readonly ChainOptions _chainOptions;
        //public ILocalEventBus LocalEventBus { get; set; }
        public CrossChainDataProvider(ICrossChainContractReader crossChainContractReader, ICrossChainDataConsumer crossChainDataConsumer)
        {
            _crossChainContractReader = crossChainContractReader;
            //_producerConsumerService = producerConsumerService;
            //_multiChainBlockInfoCache = multiChainBlockInfoCache;
            _crossChainDataConsumer = crossChainDataConsumer;
            //_chainOptions = options.Value;
            //LocalEventBus = NullLocalEventBus.Instance;
            //_newSideChainDataProducerCreationHandler += clientService.CreateConsumerProducer;
            //LocalEventBus.Subscribe<NewChainEvent>(OnNewSideChainConnectionReceivedEvent);
        }

        public async Task<bool> GetSideChainBlockDataAsync(int chainId, IList<SideChainBlockData> sideChainBlockData,
            Hash previousBlockHash, ulong preBlockHeight, bool isValidation = false)
        {
            if (!isValidation)
            {
                // this happens before mining
                if (sideChainBlockData.Count > 0)
                    return false;
                var dict = await _crossChainContractReader.GetSideChainIdAndHeightAsync(chainId, previousBlockHash,
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
                var targetHeight = await _crossChainContractReader.GetSideChainCurrentHeightAsync(chainId,
                    blockInfo.ChainId, previousBlockHash, preBlockHeight);
                if (targetHeight != blockInfo.Height)
                    // this should not happen if it is good data.
                    return false;
                
                var cachedBlockInfo = _crossChainDataConsumer.TryTake(blockInfo.ChainId, targetHeight, false);
                if (cachedBlockInfo == null || !cachedBlockInfo.Equals(blockInfo))
                    return false;
            }
            return true;
        }

        public async Task<bool> GetParentChainBlockDataAsync(int chainId,
            IList<ParentChainBlockData> parentChainBlockData, Hash previousBlockHash, ulong preBlockHeight,
            bool isValidation = false)
        {
            if (!isValidation && parentChainBlockData.Count > 0)
                return false;
            var parentChainId = await _crossChainContractReader.GetParentChainIdAsync(chainId, previousBlockHash, preBlockHeight);
            if (parentChainId == 0)
                // no configured parent chain
                return false;
            
            // Size of result is GlobalConfig.MaximalCountForIndexingParentChainBlock if it is mining process.
            if (isValidation && parentChainBlockData.Count > CrossChainConsts.MaximalCountForIndexingParentChainBlock)
                return false;
            
            int length = isValidation ? parentChainBlockData.Count : CrossChainConsts.MaximalCountForIndexingParentChainBlock ;
            
            int i = 0;
            ulong targetHeight = await _crossChainContractReader.GetParentChainCurrentHeightAsync(chainId, previousBlockHash, preBlockHeight);
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

//        private Task OnNewSideChainConnectionReceivedEvent(NewChainEvent @event)
//        {
//            _newSideChainDataProducerCreationHandler(@event.CrossChainDataProducer);
//            return Task.CompletedTask;
//        }

//        public int GetCachedBlockDataCount(int chainId)
//        {
//            if(_sideChainBlockDataConsumers.TryGetValue(chainId, out var blockInfoCache))
//                return blockInfoCache;
//        }


//        public async Task HandleEventAsync(NewChainEvent eventData)
//        {
//            var blockInfoCache = new BlockInfoCache();
//            var dto = new CommunicationContextDto
//            {
//                CrossChainCommunicationContext = eventData.CrossChainCommunicationContext,
//                BlockInfoCache = blockInfoCache,
//                TargetHeight = await _crossChainContractReader.GetSideChainCurrentHeightAsync(eventData.LocalChainId, 
//                    eventData.CrossChainCommunicationContext.ChainId, TODO, TODO)
//            };
//            var (consumer, _) = _producerConsumerService.CreateConsumerProducer(dto);
//            if (eventData.CrossChainCommunicationContext.IsSideChain)
//                _sideChainBlockDataConsumers.Add(eventData.CrossChainDataProducer.ChainId, consumer);
//            else
//                ParentChainBlockDataConsumer = consumer;
//        }

//        public Task HandleEventAsync(NewParentChainEvent eventData)
//        {
//            ParentChainBlockDataConsumer = eventData.CrossChainDataConsumer;
//            return Task.CompletedTask;
//        }
    }
}
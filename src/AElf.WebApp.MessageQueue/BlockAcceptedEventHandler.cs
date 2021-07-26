using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;

namespace AElf.WebApp.MessageQueue
{
    public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ISingletonDependency
    {
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly IBlockchainService _blockchainService;
        private readonly IMessageFilterService _messageFilterService;
        private readonly MessageQueueOptions _messageQueueOptions;
        private readonly ChainOptions _chainOptions;
        private readonly IBackgroundJobManager _parallelQueue;
        private readonly IEventInParallelProvider _eventInParallelProvider;

        public ILogger<BlockAcceptedEventHandler> Logger { get; set; }

        private List<BlockExecutedSet> _blockExecutedSets;

        public BlockAcceptedEventHandler(IDistributedEventBus distributedEventBus,
            IBlockchainService blockchainService,
            IOptionsSnapshot<MessageQueueOptions> messageQueueEnableOptions,
            IMessageFilterService messageFilterService,
            IOptionsSnapshot<ChainOptions> chainOptions,
            IBackgroundJobManager parallelQueue, IEventInParallelProvider eventInParallelProvider)
        {
            _distributedEventBus = distributedEventBus;
            _blockchainService = blockchainService;
            _messageFilterService = messageFilterService;
            _parallelQueue = parallelQueue;
            _eventInParallelProvider = eventInParallelProvider;
            _messageQueueOptions = messageQueueEnableOptions.Value;
            Logger = NullLogger<BlockAcceptedEventHandler>.Instance;
            _blockExecutedSets = new List<BlockExecutedSet>();
            _chainOptions = chainOptions.Value;
        }

        public async Task HandleEventAsync(BlockAcceptedEvent eventData)
        {
            var chain = await _blockchainService.GetChainAsync();
            if (!_messageQueueOptions.Enable ||
                chain.BestChainHeight < _messageQueueOptions.StartPublishMessageHeight)
                return;

            _blockExecutedSets.Add(_messageFilterService.GetPublishBlockExecutedSet(eventData.BlockExecutedSet));

            Logger.LogInformation(
                $"Add new block info of height {eventData.Block.Height} to list, current list length: {_blockExecutedSets.Count}");
            if (_blockExecutedSets.Count < _messageQueueOptions.PublishStep)
            {
                return;
            }

            var (serialHandleTxResultList, parallelHandleTxResultList) = GetTransactionResultListEto(
                _blockExecutedSets, _chainOptions.ChainId);
            try
            {
                if (serialHandleTxResultList.TransactionResults.Any())
                {
                    Logger.LogInformation("Start publish serial log events.");
                    await _distributedEventBus.PublishAsync(serialHandleTxResultList);
                    Logger.LogInformation("End publish serial log events.");

                    Logger.LogInformation(
                        $"Messages of block height from {serialHandleTxResultList.StartBlockNumber} to {serialHandleTxResultList.EndBlockNumber} sent to serial queue. " +
                        $"Totally {serialHandleTxResultList.TransactionResults.Values.Sum(t => t.Logs.Length)} log events.");
                }

                if (parallelHandleTxResultList.TransactionResults.Any())
                {
                    Logger.LogInformation("Start publish parallel log events.");
                    await _parallelQueue.EnqueueAsync(parallelHandleTxResultList);
                    Logger.LogInformation("End publish parallel log events.");

                    Logger.LogInformation(
                        $"Messages of block height from {parallelHandleTxResultList.StartBlockNumber} to {parallelHandleTxResultList.EndBlockNumber} sent to parallel queue. " +
                        $"Totally {parallelHandleTxResultList.TransactionResults.Values.Sum(t => t.Logs.Length)} log events.");
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to publish events to mq service.\n{e.Message}");
            }

            _blockExecutedSets = new List<BlockExecutedSet>();
        }

        private (TransactionResultListEto, TransactionResultListEto) GetTransactionResultListEto(
            IReadOnlyCollection<BlockExecutedSet> blockExecutedSets, int chainId)
        {
            var startBlockNumber = blockExecutedSets.First().Height;
            var endBlockNumber = blockExecutedSets.Last().Height;
            var serialHandleTxResultList = new TransactionResultListEto
            {
                TransactionResults = new Dictionary<string, TransactionResultEto>(),
                StartBlockNumber = startBlockNumber,
                EndBlockNumber = endBlockNumber,
                ChainId = chainId
            };

            var parallelHandleTxResultList = new TransactionResultListEto
            {
                TransactionResults = new Dictionary<string, TransactionResultEto>(),
                StartBlockNumber = startBlockNumber,
                EndBlockNumber = endBlockNumber,
                ChainId = chainId
            };

            foreach (var blockExecutedInfo in blockExecutedSets)
            {
                foreach (var (txId, txResult) in blockExecutedInfo.TransactionResultMap)
                {
                    var logs = txResult.Logs.Select(l => new LogEventEto
                    {
                        Address = l.Address.ToBase58(),
                        Name = l.Name,
                        Indexed = l.Indexed.Select(i => i.ToBase64()).ToArray(),
                        NonIndexed = l.NonIndexed.ToBase64()
                    }).ToArray();
                    
                    var parallelHandleEventEtos = new List<LogEventEto>();
                    var serialHandleEventEtos = new List<LogEventEto>();
                    foreach (var log in logs)
                    {
                        if (_eventInParallelProvider.IsEventHandleParallel(log))
                            parallelHandleEventEtos.Add(log);
                        else
                            serialHandleEventEtos.Add(log);
                    }

                    if (serialHandleEventEtos.Any())
                    {
                        
                        serialHandleTxResultList.TransactionResults.Add(
                            txId.ToHex(), GenerateTransactionResultEto(blockExecutedInfo.Block,
                                blockExecutedInfo.TransactionMap[txId], txResult, serialHandleEventEtos));
                    }

                    if (parallelHandleEventEtos.Any())
                    {
                        parallelHandleTxResultList.TransactionResults.Add(
                            txId.ToHex(),
                            GenerateTransactionResultEto(blockExecutedInfo.Block,
                                blockExecutedInfo.TransactionMap[txId], txResult, parallelHandleEventEtos));
                    }
                }
            }
            return (serialHandleTxResultList, parallelHandleTxResultList);
        }

        private TransactionResultEto GenerateTransactionResultEto(IBlock block, Transaction tx,
            TransactionResult txResult, List<LogEventEto> logList)
        {
            return new TransactionResultEto
            {
                TransactionId = txResult.TransactionId.ToHex(),
                Status = txResult.Status.ToString().ToUpper(),
                Error = txResult.Error,
                Logs = logList.ToArray(),
                ReturnValue = txResult.ReturnValue.ToHex(),
                BlockNumber = txResult.BlockNumber,
                MethodName = tx.MethodName,
                BlockTime = block.Header.Time.ToDateTime()
            };
        }
    }
}
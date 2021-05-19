using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;

namespace AElf.WebApp.MessageQueue
{
    public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ISingletonDependency
    {
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly IBlockchainService _blockchainService;
        private readonly MessageQueueOptions _messageQueueOptions;
        public ILogger<BlockAcceptedEventHandler> Logger { get; set; }

        private List<BlockExecutedSet> _blockExecutedSets;

        public BlockAcceptedEventHandler(IDistributedEventBus distributedEventBus,
            IBlockchainService blockchainService,
            IOptionsSnapshot<MessageQueueOptions> messageQueueEnableOptions)
        {
            _distributedEventBus = distributedEventBus;
            _blockchainService = blockchainService;
            _messageQueueOptions = messageQueueEnableOptions.Value;
            Logger = NullLogger<BlockAcceptedEventHandler>.Instance;
            _blockExecutedSets = new List<BlockExecutedSet>();
        }

        public async Task HandleEventAsync(BlockAcceptedEvent eventData)
        {
            var chain = await _blockchainService.GetChainAsync();
            if (!_messageQueueOptions.Enable ||
                chain.BestChainHeight < _messageQueueOptions.StartPublishMessageHeight)
                return;

            if (_blockExecutedSets.Count < _messageQueueOptions.PublishStep)
            {
                Logger.LogInformation(
                    $"Add new block info of height {eventData.Block.Height} to list, current list length: {_blockExecutedSets.Count}");
                _blockExecutedSets.Add(eventData.BlockExecutedSet);
                return;
            }

            var txResultList = new TransactionResultListEto
            {
                TransactionResults = new Dictionary<string, TransactionResultEto>(),
                StartBlockNumber = _blockExecutedSets.First().Height,
                EndBlockNumber = _blockExecutedSets.Last().Height
            };
            foreach (var resultPair in _blockExecutedSets.SelectMany(s => s.TransactionResultMap))
            {
                txResultList.TransactionResults.Add(resultPair.Key.ToHex(), new TransactionResultEto
                {
                    TransactionId = resultPair.Value.TransactionId.ToHex(),
                    Status = resultPair.Value.Status.ToString().ToUpper(),
                    Error = resultPair.Value.Error,
                    Logs = resultPair.Value.Logs.Select(l => new LogEventEto
                    {
                        Address = l.Address.ToBase58(),
                        Name = l.Name,
                        Indexed = l.Indexed.Select(i => i.ToBase64()).ToArray(),
                        NonIndexed = l.NonIndexed.ToBase64()
                    }).ToArray(),
                    ReturnValue = resultPair.Value.ReturnValue.ToHex()
                });
            }

            try
            {
                Logger.LogInformation("Start publish log events.");
                await _distributedEventBus.PublishAsync(txResultList);
                Logger.LogInformation("End publish log events.");

                Logger.LogInformation(
                    $"Messages of block height from {txResultList.StartBlockNumber} to {txResultList.EndBlockNumber} sent. " +
                    $"Totally {txResultList.TransactionResults.Values.Sum(t => t.Logs.Length)} log events.");
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to publish events to mq service.\n{e.Message}");
            }

            _blockExecutedSets = new List<BlockExecutedSet>();
        }
    }
}
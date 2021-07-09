using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
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
        private readonly IMessageFilterService _messageFilterService;
        private readonly MessageQueueOptions _messageQueueOptions;
        private readonly ChainOptions _chainOptions;
        public ILogger<BlockAcceptedEventHandler> Logger { get; set; }

        private List<BlockExecutedSet> _blockExecutedSets;

        public BlockAcceptedEventHandler(IDistributedEventBus distributedEventBus,
            IBlockchainService blockchainService,
            IOptionsSnapshot<MessageQueueOptions> messageQueueEnableOptions,
            IMessageFilterService messageFilterService,
            IOptionsSnapshot<ChainOptions> chainOptions)
        {
            _distributedEventBus = distributedEventBus;
            _blockchainService = blockchainService;
            _messageFilterService = messageFilterService;
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

            var txResultList = new TransactionResultListEto
            {
                TransactionResults = new Dictionary<string, TransactionResultEto>(),
                StartBlockNumber = _blockExecutedSets.First().Height,
                EndBlockNumber = _blockExecutedSets.Last().Height,
                ChainId = _chainOptions.ChainId
            };

            foreach (var (txId, txResult) in _blockExecutedSets.SelectMany(s => s.TransactionResultMap))
            {
                txResultList.TransactionResults.Add(txId.ToHex(), new TransactionResultEto
                {
                    TransactionId = txResult.TransactionId.ToHex(),
                    Status = txResult.Status.ToString().ToUpper(),
                    Error = txResult.Error,
                    Logs = txResult.Logs.Select(l => new LogEventEto
                    {
                        Address = l.Address.ToBase58(),
                        Name = l.Name,
                        Indexed = l.Indexed.Select(i => i.ToBase64()).ToArray(),
                        NonIndexed = l.NonIndexed.ToBase64()
                    }).ToArray(),
                    ReturnValue = txResult.ReturnValue.ToHex(),
                    BlockNumber = txResult.BlockNumber
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
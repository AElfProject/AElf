using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;

namespace AElf.WebApp.MessageQueue
{
    public class BlockAcceptedEventHandler : ILocalEventHandler<BlockAcceptedEvent>, ITransientDependency
    {
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly MessageQueueOptions _messageQueueOptions;
        public ILogger<BlockAcceptedEventHandler> Logger { get; set; }

        public BlockAcceptedEventHandler(IDistributedEventBus distributedEventBus,
            IOptionsSnapshot<MessageQueueOptions> messageQueueEnableOptions)
        {
            _distributedEventBus = distributedEventBus;
            _messageQueueOptions = messageQueueEnableOptions.Value;
            Logger = NullLogger<BlockAcceptedEventHandler>.Instance;
        }

        public async Task HandleEventAsync(BlockAcceptedEvent eventData)
        {
            if (!_messageQueueOptions.Enable) return;

            Logger.LogInformation($"Message of block height {eventData.Block.Height} sent.");
            var txResultList = new TransactionResultListEto
            {
                TransactionResults = new Dictionary<string, TransactionResultEto>()
            };
            foreach (var resultPair in eventData.BlockExecutedSet.TransactionResultMap)
            {
                txResultList.TransactionResults.Add(resultPair.Key.ToHex(), new TransactionResultEto
                {
                    TransactionId = resultPair.Value.TransactionId.ToHex(),
                    BlockHash = resultPair.Value.BlockHash.ToHex(),
                    BlockNumber = resultPair.Value.BlockNumber,
                    Bloom = resultPair.Value.Status == TransactionResultStatus.NotExisted ? null :
                        resultPair.Value.Bloom.Length == 0 ? ByteString.CopyFrom(new byte[256]).ToBase64() :
                        resultPair.Value.Bloom.ToBase64(),
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
                await _distributedEventBus.PublishAsync(txResultList);
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to publish events to mq service.\n{e.Message}");
            }
        }
    }
}
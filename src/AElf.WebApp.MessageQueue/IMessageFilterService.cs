using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.Blockchain;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue
{
    public interface IMessageFilterService
    {
        BlockExecutedSet GetPublishBlockExecutedSet(BlockExecutedSet originBlockExecutedSet);
    }

    public class MessageFilterService : IMessageFilterService, ITransientDependency
    {
        private readonly MessageQueueOptions _messageQueueOptions;

        public MessageFilterService(IOptionsSnapshot<MessageQueueOptions> messageQueueOptions)
        {
            _messageQueueOptions = messageQueueOptions.Value;
        }

        public BlockExecutedSet GetPublishBlockExecutedSet(BlockExecutedSet originBlockExecutedSet)
        {
            var isFromMatch = new Lazy<Func<List<string>, Hash, bool>>((fromAddressList, txHash) =>
                fromAddressList.Contains(originBlockExecutedSet
                    .TransactionMap[txHash].From.ToBase58()));
            var isToMatch = new Lazy<Func<List<string>, Hash, bool>>((toAddressList, txHash) =>
                toAddressList.Contains(originBlockExecutedSet.TransactionMap[txHash].To.ToBase58()));
            var isEventNameMatch = new Lazy<Func<List<string>, TransactionResult, bool>>((eventNameList, txResult) =>
                txResult.Logs.Any(logEvent => eventNameList.Contains(logEvent.Name)));

            var resultSet = new BlockExecutedSet
            {
                Block = originBlockExecutedSet.Block,
                TransactionResultMap = _messageQueueOptions.MessageFilter.Mode switch
                {
                    MessageFilterMode.OnlyTo => originBlockExecutedSet.TransactionResultMap
                        .Where(r => isToMatch.Value(_messageQueueOptions.MessageFilter.ToAddresses, r.Key))
                        .ToDictionary(p => p.Key, p => p.Value),
                    MessageFilterMode.OnlyFrom => originBlockExecutedSet.TransactionResultMap
                        .Where(r => isFromMatch.Value(_messageQueueOptions.MessageFilter.FromAddresses, r.Key))
                        .ToDictionary(p => p.Key, p => p.Value),
                    MessageFilterMode.OnlyEventName => originBlockExecutedSet.TransactionResultMap
                        .Where(r => isEventNameMatch.Value(_messageQueueOptions.MessageFilter.EventNames, r.Value))
                        .ToDictionary(p => p.Key, p => p.Value),
                    MessageFilterMode.ToAndEventName => originBlockExecutedSet.TransactionResultMap
                        .Where(r => isToMatch.Value(_messageQueueOptions.MessageFilter.ToAddresses, r.Key) &&
                                    isEventNameMatch.Value(_messageQueueOptions.MessageFilter.EventNames, r.Value))
                        .ToDictionary(p => p.Key, p => p.Value),
                    MessageFilterMode.FromAndEventName => originBlockExecutedSet.TransactionResultMap
                        .Where(r => isFromMatch.Value(_messageQueueOptions.MessageFilter.FromAddresses, r.Key) &&
                                    isEventNameMatch.Value(_messageQueueOptions.MessageFilter.EventNames, r.Value))
                        .ToDictionary(p => p.Key, p => p.Value),
                    MessageFilterMode.FromAndTo => originBlockExecutedSet.TransactionResultMap
                        .Where(r => isFromMatch.Value(_messageQueueOptions.MessageFilter.FromAddresses, r.Key) &&
                                    isToMatch.Value(_messageQueueOptions.MessageFilter.ToAddresses, r.Key))
                        .ToDictionary(p => p.Key, p => p.Value),
                    MessageFilterMode.All => originBlockExecutedSet.TransactionResultMap.Where(r =>
                            isFromMatch.Value(_messageQueueOptions.MessageFilter.FromAddresses, r.Key) &&
                            isToMatch.Value(_messageQueueOptions.MessageFilter.ToAddresses, r.Key) &&
                            isEventNameMatch.Value(_messageQueueOptions.MessageFilter.EventNames, r.Value))
                        .ToDictionary(p => p.Key, p => p.Value),
                    _ => throw new ArgumentOutOfRangeException()
                }
            };

            return resultSet;
        }
    }
}
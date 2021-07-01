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
                txResult.Logs
                    .Select(logEvent => eventNameList.Contains(logEvent.Name)).FirstOrDefault());
            switch (_messageQueueOptions.MessageFilter.Mode)
            {
                case MessageFilterMode.OnlyTo:
                    return new BlockExecutedSet
                    {
                        TransactionResultMap = originBlockExecutedSet.TransactionResultMap
                            .Where(r => isToMatch.Value(_messageQueueOptions.MessageFilter.ToAddresses, r.Key))
                            .ToDictionary(p => p.Key, p => p.Value)
                    };
                case MessageFilterMode.OnlyFrom:
                    return new BlockExecutedSet
                    {
                        TransactionResultMap = originBlockExecutedSet.TransactionResultMap
                            .Where(r => isFromMatch.Value(_messageQueueOptions.MessageFilter.FromAddresses, r.Key))
                            .ToDictionary(p => p.Key, p => p.Value)
                    };
                case MessageFilterMode.OnlyEventName:
                    return new BlockExecutedSet
                    {
                        TransactionResultMap = originBlockExecutedSet.TransactionResultMap
                            .Where(r => isEventNameMatch.Value(_messageQueueOptions.MessageFilter.EventNames, r.Value))
                            .ToDictionary(p => p.Key, p => p.Value)
                    };
                case MessageFilterMode.ToAndEventName:
                    return new BlockExecutedSet
                    {
                        TransactionResultMap = originBlockExecutedSet.TransactionResultMap
                            .Where(r => isToMatch.Value(_messageQueueOptions.MessageFilter.ToAddresses, r.Key) &&
                                        isEventNameMatch.Value(_messageQueueOptions.MessageFilter.EventNames, r.Value))
                            .ToDictionary(p => p.Key, p => p.Value)
                    };
                case MessageFilterMode.FromAndEventName:
                    return new BlockExecutedSet
                    {
                        TransactionResultMap = originBlockExecutedSet.TransactionResultMap
                            .Where(r => isFromMatch.Value(_messageQueueOptions.MessageFilter.FromAddresses, r.Key) &&
                                        isEventNameMatch.Value(_messageQueueOptions.MessageFilter.EventNames, r.Value))
                            .ToDictionary(p => p.Key, p => p.Value)
                    };
                case MessageFilterMode.FromAndTo:
                    return new BlockExecutedSet
                    {
                        TransactionResultMap = originBlockExecutedSet.TransactionResultMap
                            .Where(r => isFromMatch.Value(_messageQueueOptions.MessageFilter.FromAddresses, r.Key) &&
                                        isToMatch.Value(_messageQueueOptions.MessageFilter.ToAddresses, r.Key))
                            .ToDictionary(p => p.Key, p => p.Value)
                    };
                case MessageFilterMode.All:
                    return new BlockExecutedSet
                    {
                        TransactionResultMap = originBlockExecutedSet.TransactionResultMap
                            .Where(r => isFromMatch.Value(_messageQueueOptions.MessageFilter.FromAddresses, r.Key) &&
                                        isToMatch.Value(_messageQueueOptions.MessageFilter.ToAddresses, r.Key) &&
                                        isEventNameMatch.Value(_messageQueueOptions.MessageFilter.EventNames, r.Value))
                            .ToDictionary(p => p.Key, p => p.Value)
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
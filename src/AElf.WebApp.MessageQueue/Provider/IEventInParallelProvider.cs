using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue
{
    public interface IEventInParallelProvider
    {
        bool IsEventHandleParallel(LogEventEto logEventEto);
    }

    public class EventInParallelProvider : IEventInParallelProvider, ISingletonDependency
    {
        private readonly Dictionary<string, HashSet<string>> _isEventInParallelQueueDic;

        public EventInParallelProvider(IOptionsSnapshot<EventHandleOptions> eventHandleOptions)
        {
            _isEventInParallelQueueDic = InitializeParallelQueueEvent(eventHandleOptions.Value);
        }

        public bool IsEventHandleParallel(LogEventEto logEventEto)
        {
            return _isEventInParallelQueueDic.TryGetValue(logEventEto.Address, out var eventsSet) &&
                   eventsSet.Contains(logEventEto.Name);
        }

        private Dictionary<string, HashSet<string>> InitializeParallelQueueEvent(EventHandleOptions option)
        {
            var eventInParallelQueueDictionary = new Dictionary<string, HashSet<string>>();
            foreach (var contract in option.ParallelHandleEventInfo)
            {
                eventInParallelQueueDictionary.TryAdd(contract.ContractName, new HashSet<string>());
                foreach (var eventName in contract.EventNames)
                {
                    eventInParallelQueueDictionary[contract.ContractName].Add(eventName);
                }
            }

            return eventInParallelQueueDictionary;
        }
    }
}
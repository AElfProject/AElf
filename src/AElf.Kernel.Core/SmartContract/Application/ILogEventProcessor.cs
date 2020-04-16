using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ILogEventProcessor
    {
        Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext);
        Task ProcessAsync(Block block, Dictionary<TransactionResult, List<LogEvent>> logEventsMap);
    }

    public class InterestedEvent
    {
        public LogEvent LogEvent { get; set; }
        public Bloom Bloom { get; set; }
    }

    public interface IBlockAcceptedLogEventProcessor : ILogEventProcessor
    {
    }

    public interface IBlocksExecutionSucceededLogEventProcessor : ILogEventProcessor
    {
    }

    public abstract class LogEventProcessorSpecialBase : LogEventProcessorBase
    {
        public override async Task ProcessAsync(Block block, Dictionary<TransactionResult, List<LogEvent>> logEventsMap)
        {
            foreach (var logEvent in logEventsMap.Values.SelectMany(logEvents => logEvents))
            {
                await ProcessLogEventAsync(block, logEvent);
            }
        }

        protected abstract Task ProcessLogEventAsync(Block block, LogEvent logEvent);
    }

    public abstract class LogEventProcessorBase : ILogEventProcessor
    {
        protected InterestedEvent InterestedEvent;

        public abstract Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext);

        public abstract Task ProcessAsync(Block block, Dictionary<TransactionResult, List<LogEvent>> logEventsMap);
        
        protected InterestedEvent GetInterestedEvent<T>(Address address) where T : IEvent<T>, new()
        {
            var logEvent = new T().ToLogEvent(address);
            return new InterestedEvent
            {
                LogEvent = logEvent,
                Bloom = logEvent.GetBloom()
            };
        }
    }
}
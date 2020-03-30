using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ILogEventProcessor
    {
        LogEvent InterestedEvent { get; }
        Task ProcessAsync(Block block, Dictionary<TransactionResult, List<LogEvent>> logEventsMap);
    }

    public interface IBlockAcceptedLogEventProcessor : ILogEventProcessor
    {

    }

    public interface IBestChainFoundLogEventProcessor : ILogEventProcessor
    {

    }

    public abstract class LogEventProcessorBase : ILogEventProcessor
    {
        public abstract LogEvent InterestedEvent { get; }

        public async Task ProcessAsync(Block block, Dictionary<TransactionResult, List<LogEvent>> logEventsMap)
        {
            foreach (var logEvent in logEventsMap.Values.SelectMany(logEvents => logEvents))
            {
                await ProcessLogEventAsync(block, logEvent);
            }
        }

        protected abstract Task ProcessLogEventAsync(Block block, LogEvent logEvent);
    }
}
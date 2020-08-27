using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ITestLogEventProcessor : ILogEventProcessor
    {
        Dictionary<long, Dictionary<TransactionResult, List<LogEvent>>> GetProcessedResult();

        void CleanProcessedResult();
    }

    public class TestLogEventProcessor : ITestLogEventProcessor
    {
        private readonly LogEvent _logEvent = new LogEvent
        {
            Name = "TestLogEvent",
            Address = SampleAddress.AddressList[0]
        };

        private readonly Dictionary<long, Dictionary<TransactionResult, List<LogEvent>>> _processedResult =
            new Dictionary<long, Dictionary<TransactionResult, List<LogEvent>>>();

        public Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
        {
            return Task.FromResult(new InterestedEvent
            {
                LogEvent = _logEvent,
                Bloom = _logEvent.GetBloom()
            });
        }

        public Task ProcessAsync(Block block, Dictionary<TransactionResult, List<LogEvent>> logEventsMap)
        {
            _processedResult.Add(block.Height, logEventsMap);
            return Task.CompletedTask;
        }
        public Dictionary<long, Dictionary<TransactionResult, List<LogEvent>>> GetProcessedResult()
        {
            return _processedResult;
        }

        public void CleanProcessedResult()
        {
            _processedResult.Clear();
        }
    }
}
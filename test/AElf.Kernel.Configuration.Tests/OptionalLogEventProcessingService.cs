using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.Configuration.Tests
{
    public class OptionalLogEventProcessingService<T> : ILogEventProcessingService<T> where T : ILogEventProcessor
    {
        private LogEventProcessingService<T> _inner;

        public OptionalLogEventProcessingService(LogEventProcessingService<T> inner)
        {
            _inner = inner;
        }

        public static bool Enabled { get; set; }

        public async Task ProcessAsync(List<BlockExecutedSet> blockExecutedSets)
        {
            if (Enabled)
            {
                await _inner.ProcessAsync(blockExecutedSets);
            }
        }
    }
}
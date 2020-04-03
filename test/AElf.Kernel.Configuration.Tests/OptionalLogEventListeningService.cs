using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.Configuration.Tests
{
    public class OptionalLogEventListeningService<T> : ILogEventListeningService<T> where T : ILogEventProcessor
    {
        private LogEventListeningService<T> _inner;

        public OptionalLogEventListeningService(LogEventListeningService<T> inner)
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
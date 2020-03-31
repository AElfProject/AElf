using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContract.Application
{
    public class LogEventListeningService<T> : ILogEventListeningService<T>
        where T : ILogEventProcessor
    {
        private Dictionary<LogEvent, Bloom> _blooms;

        private Dictionary<LogEvent, Bloom> Blooms => _blooms ??= _logEventProcessors.Select(h => h.InterestedEvent)
            .ToDictionary(e => e, e => e.GetBloom());

        private readonly List<T> _logEventProcessors;

        public ILogger<LogEventListeningService<T>> Logger { get; set; }

        public LogEventListeningService(IServiceContainer<T> logEventProcessors)
        {
            _logEventProcessors = logEventProcessors.ToLookup(p => p.GetType()).Select(coll => coll.First()).ToList();
        }

        public async Task ProcessAsync(List<BlockExecutedSet> blockExecutedSets)
        {
            Logger.LogTrace("Apply log event processor.");
            foreach (var executedSet in blockExecutedSets)
            {
                var block = executedSet.Block;
                var txResults = executedSet.TransactionResultMap.Values;

                if (!txResults.Any()) continue;

                foreach (var processor in _logEventProcessors)
                {
                    var logEventsMap = new Dictionary<TransactionResult, List<LogEvent>>();
                    var blockBloom = new Bloom(block.Header.Bloom.ToByteArray());
                    if (!Blooms.Values.Any(b => b.IsIn(blockBloom)))
                    {
                        // No interested event in the block
                        continue;
                    }

                    foreach (var result in txResults)
                    {
                        if (result.Bloom.Length == 0) continue;
                        result.BlockHash = block.GetHash();
                        var resultBloom = new Bloom(result.Bloom.ToByteArray());
                        var interestedEvent = processor.InterestedEvent;
                        var interestedBloom = Blooms[interestedEvent];
                        if (!interestedBloom.IsIn(resultBloom))
                        {
                            // Interested bloom is not found in the transaction result
                            continue;
                        }

                        // Interested bloom is found in the transaction result,
                        // find the log that yields the bloom and apply the processor
                        foreach (var log in result.Logs)
                        {
                            if (log.Address != interestedEvent.Address || log.Name != interestedEvent.Name) continue;
                            if (logEventsMap.ContainsKey(result))
                            {
                                logEventsMap[result].Add(log);
                            }
                            else
                            {
                                logEventsMap[result] = new List<LogEvent>
                                {
                                    log
                                };
                            }
                        }
                    }

                    await processor.ProcessAsync(block, logEventsMap.ToDictionary(m => m.Key, m => m.Value));
                }

                Logger.LogTrace("Finish apply log event processor.");
            }
        }
    }
}
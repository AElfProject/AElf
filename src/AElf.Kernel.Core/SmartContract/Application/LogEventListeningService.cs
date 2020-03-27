using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContract.Application
{
    public class LogEventListeningService<T> : ILogEventListeningService<T>
        where T : ILogEventProcessor
    {
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        private Dictionary<LogEvent, Bloom> _blooms;

        private Dictionary<LogEvent, Bloom> Blooms => _blooms ??= _logEventProcessors.Select(h => h.InterestedEvent)
            .ToDictionary(e => e, e => e.GetBloom());

        private readonly List<T> _logEventProcessors;

        public ILogger<LogEventListeningService<T>> Logger { get; set; }

        public LogEventListeningService(ITransactionResultQueryService transactionResultQueryService,
            IServiceContainer<T> logEventProcessors)
        {
            _transactionResultQueryService = transactionResultQueryService;
            _logEventProcessors = logEventProcessors.ToLookup(p => p.GetType()).Select(coll => coll.First()).ToList();
        }

        public async Task ProcessAsync(IEnumerable<Block> blocks)
        {
            Logger.LogTrace("Apply log event processor.");
            foreach (var block in blocks)
            {
                foreach (var processor in _logEventProcessors)
                {
                    var logEventsMap = new Dictionary<TransactionResult, ConcurrentBag<LogEvent>>();
                    var blockBloom = new Bloom(block.Header.Bloom.ToByteArray());
                    if (!Blooms.Values.Any(b => b.IsIn(blockBloom)))
                    {
                        // No interested event in the block
                        continue;
                    }

                    var txResults =
                        await _transactionResultQueryService.GetTransactionResultsAsync(block.Body.TransactionIds,
                            block.GetHash());
                    if (txResults == null || !txResults.Any()) continue;

                    foreach (var result in txResults.AsParallel())
                    {
                        if (result.Bloom.Length == 0) continue;
                        result.BlockHash = block.GetHash();
                        var resultBloom = new Bloom(result.Bloom.ToByteArray());

                        {
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
                                if (log.Address == interestedEvent.Address && log.Name == interestedEvent.Name)
                                {
                                    if (logEventsMap.ContainsKey(result))
                                    {
                                        logEventsMap[result].Add(log);
                                    }
                                    else
                                    {
                                        logEventsMap[result] = new ConcurrentBag<LogEvent>
                                        {
                                            log
                                        };
                                    }
                                }
                            }
                        }
                    }

                    await processor.ProcessAsync(block, logEventsMap.ToDictionary(m => m.Key, m => m.Value.ToList()));
                }

                Logger.LogTrace("Finish apply log event processor.");
            }
        }
    }
}
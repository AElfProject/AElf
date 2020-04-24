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
                // Should make sure tx results' order are same as tx ids in block body.
                var txResults = executedSet.TransactionResultMap.Values
                    .OrderBy(d => block.Body.TransactionIds.IndexOf(d.TransactionId)).ToList();

                if (!txResults.Any()) continue;

                var blockBloom = new Bloom(block.Header.Bloom.ToByteArray());
                var chainContext = new ChainContext
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Height
                };

                foreach (var processor in _logEventProcessors)
                {
                    var interestedEvent = await processor.GetInterestedEventAsync(chainContext);
                    if(interestedEvent == null || !interestedEvent.Bloom.IsIn(blockBloom)) continue;
                    
                    var logEventsMap = new Dictionary<TransactionResult, List<LogEvent>>();
                    foreach (var result in txResults)
                    {
                        if (result.Bloom.Length == 0) continue;
                        var resultBloom = new Bloom(result.Bloom.ToByteArray());
                        
                        if (!interestedEvent.Bloom.IsIn(resultBloom))
                        {
                            // Interested bloom is not found in the transaction result
                            continue;
                        }

                        // Interested bloom is found in the transaction result,
                        // find the log that yields the bloom and apply the processor
                        foreach (var log in result.Logs)
                        {
                            if (log.Address != interestedEvent.LogEvent.Address || log.Name != interestedEvent.LogEvent.Name) continue;
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
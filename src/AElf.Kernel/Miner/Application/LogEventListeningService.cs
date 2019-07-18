using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Miner.Application
{
    public class LogEventListeningService : ILogEventListeningService, ISingletonDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        private Dictionary<LogEvent, Bloom> _blooms;

        private Dictionary<LogEvent, Bloom> Blooms =>
            _blooms ??
            (_blooms = _eventHandlers.Select(h => h.InterestedEvent).ToDictionary(e => e, e => e.GetBloom()));

        private readonly List<ILogEventHandler> _eventHandlers;

        public LogEventListeningService(IBlockchainService blockchainService,
            ITransactionResultQueryService transactionResultQueryService,
            IServiceContainer<ILogEventHandler> eventHandlers)
        {
            _blockchainService = blockchainService;
            _transactionResultQueryService = transactionResultQueryService;
            _eventHandlers = eventHandlers.ToLookup(p => p.GetType()).Select(coll => coll.First()).ToList();
        }

        public async Task ApplyAsync(IEnumerable<Hash> blockHashes)
        {
            foreach (var blockId in blockHashes)
            {
                var block = await _blockchainService.GetBlockByHashAsync(blockId);

                var blockBloom = new Bloom(block.Header.Bloom.ToByteArray());
                if (!Blooms.Values.Any(b => b.IsIn(blockBloom)))
                {
                    // No interested event in the block
                    continue;
                }

                foreach (var transactionId in block.Body.TransactionIds)
                {
                    var result = await _transactionResultQueryService.GetTransactionResultAsync(transactionId);
                    if (result == null)
                    {
                        continue;
                    }

                    if (result.Status == TransactionResultStatus.Failed)
                    {
                        continue;
                    }

                    if (result.Bloom.Length == 0)
                    {
                        continue;
                    }

                    var resultBloom = new Bloom(result.Bloom.ToByteArray());

                    foreach (var handler in _eventHandlers)
                    {
                        var interestedEvent = handler.InterestedEvent;
                        var interestedBloom = Blooms[interestedEvent];
                        if (!interestedBloom.IsIn(resultBloom))
                        {
                            // Interested bloom is not found in the transaction result
                            continue;
                        }

                        // Interested bloom is found in the transaction result,
                        // find the log that yields the bloom and apply the handler
                        foreach (var log in result.Logs)
                        {
                            if (log.Address != interestedEvent.Address || log.Name != interestedEvent.Name)
                                continue;
                            await handler.Handle(block, result, log);
                        }
                    }
                }
            }
        }
    }
}
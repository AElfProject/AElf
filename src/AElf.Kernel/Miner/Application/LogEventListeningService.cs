using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;

namespace AElf.Kernel.Miner.Application
{
    public class LogEventListeningService : ILogEventListeningService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        private readonly Dictionary<LogEvent, Bloom> _blooms;
        private readonly List<ILogEventHandler> _eventHandlers;

        public LogEventListeningService(IBlockchainService blockchainService,
            ITransactionResultQueryService transactionResultQueryService,
            IServiceContainer<ILogEventHandler> eventHandlers)
        {
            _blockchainService = blockchainService;
            _transactionResultQueryService = transactionResultQueryService;
            _eventHandlers = eventHandlers.ToList();
            _blooms = _eventHandlers.Select(h => h.InterestedEvent).ToDictionary(e => e, e => e.GetBloom());
        }

        public async Task Apply(Chain chain, IEnumerable<Hash> blockHashes)
        {
            foreach (var blockId in blockHashes)
            {
                var block = await _blockchainService.GetBlockByHashAsync(blockId);

                var blockBloom = new Bloom(block.Header.Bloom.ToByteArray());
                if (!_blooms.Values.Any(b => b.IsIn(blockBloom)))
                {
                    // No interested event in the block
                    continue;
                }

                foreach (var transactionHash in block.Body.Transactions)
                {
                    var result = await _transactionResultQueryService.GetTransactionResultAsync(transactionHash);
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
                        var interestedBloom = _blooms[interestedEvent];
                        if (!interestedBloom.IsIn(resultBloom))
                        {
                            continue;
                        }

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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using NLog;

namespace AElf.Miner.TxMemPool
{
    // ReSharper disable InconsistentNaming
    public class TxPool : ITxPool
    {
        private readonly ILogger _logger;
        private readonly ITxValidator _txValidator;
        private int Least { get; set; }
        private int Limit { get; set; }

        public TxPool(ILogger logger, ITxValidator txValidator, NewTxHub txHub)
        {
            _logger = logger;
            _txValidator = txValidator;
            _txHub = txHub;

            _dpoSTxFilter = new DPoSTxFilter();
        }

        public void Start()
        {
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        private NewTxHub _txHub;

        private readonly DPoSTxFilter _dpoSTxFilter;

        /// <inheritdoc/>
        public async Task<TxValidation.TxInsertionAndBroadcastingError> AddTxAsync(Transaction tx, bool validateReference = true)
        {
            await _txHub.AddTransactionAsync(tx);
            return TxValidation.TxInsertionAndBroadcastingError.Success;
        }

        /// <inheritdoc/>
        public bool TryGetTx(Hash txHash, out Transaction tx)
        {
            tx = _txHub.GetTxAsync(txHash).Result;
            return tx != null;
        }

        /// <inheritdoc/>
        public async Task<List<Transaction>> GetReadyTxsAsync(Round currentRoundInfo, double intervals = 150)
        {
            var txs = (await _txHub.GetReadyTxsAsync()).GroupBy(x=>x.IsSystemTxn).ToDictionary(x=>x.Key, x=>x.Select(y=>y.Transaction).ToList());

            if (txs.TryGetValue(true, out var sysTxs))
            {
                _dpoSTxFilter.Execute(sysTxs);
            }
            _logger.Debug($"Got {sysTxs?.Count??0} System tx");
            var totalTxs = txs.Values.SelectMany(x=>x).ToList();
            _logger.Debug($"Got {totalTxs.Count} total tx");
            return totalTxs;
        }

        public void SetBlockVolume(int minimal, int maximal)
        {
            Least = minimal;
            Limit = maximal;
        }

        public async Task<ulong> GetPoolSize()
        {
            return (ulong) (await _txHub.GetReadyTxsAsync()).Count();
        }
    }
}
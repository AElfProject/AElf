using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Storages;
using NLog;
using AElf.Common;
using AElf.Database;

namespace AElf.Kernel.Managers
{
    public class TransactionManager: ITransactionManager
    {
        private readonly ITransactionStore _transactionStore;
        private readonly ILogger _logger;

        // Todo remove it later
        private readonly IDataStore _dataStore;

        public TransactionManager(ITransactionStore transactionStore)
        {
            _transactionStore = transactionStore;
            _logger = LogManager.GetLogger(nameof(TransactionManager));

            _dataStore = new DataStore(new InMemoryDatabase());
        }

        public async Task<Hash> AddTransactionAsync(Transaction tx)
        {
            var txHash = tx.GetHash();
            await _transactionStore.SetAsync(GetStringKey(txHash), tx);
            return txHash;
        }

        public async Task<Transaction> GetTransaction(Hash txId)
        {
            return await _transactionStore.GetAsync<Transaction>(GetStringKey(txId));
        }

        public async Task RemoveTransaction(Hash txId)
        {
            await _transactionStore.RemoveAsync(GetStringKey(txId));
        }
        
        public async Task<List<Transaction>> RollbackTransactions(Hash chainId, ulong currentHeight, ulong specificHeight)
        {
            var txs = new List<Transaction>();
            for (var i = currentHeight - 1; i >= specificHeight; i--)
            {
                var rollBackBlockHash = await _dataStore.GetAsync<Hash>(DataPath.CalculatePointerForGettingBlockHashByHeight(chainId, i));
                var header = await _dataStore.GetAsync<BlockHeader>(rollBackBlockHash);
                var body = await _dataStore.GetAsync<BlockBody>(Hash.Xor(header.GetHash(),header.MerkleTreeRootOfTransactions));
                foreach (var txId in body.Transactions)
                {
                    var tx = await GetTransaction(txId);
                    if (tx == null)
                    {
                        _logger.Trace($"tx {txId} is null.");
                    }
                    txs.Add(tx);
                    await RemoveTransaction(txId);
                }

                _logger.Trace($"Rollback block hash: {rollBackBlockHash.Value.ToByteArray().ToHex()}");
            }

            return txs;
        }
        
        private string GetStringKey(Hash txId)
        {
            return txId.DumpHex();
        }
    }
}
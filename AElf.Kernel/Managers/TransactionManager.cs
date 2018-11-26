using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Storages;
using NLog;
using AElf.Common;

namespace AElf.Kernel.Managers
{
    public class TransactionManager: ITransactionManager
    {
        private readonly IDataStore _dataStore;
        private readonly ILogger _logger;

        public TransactionManager(IDataStore dataStore, ILogger logger = null)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task<Hash> AddTransactionAsync(Transaction tx)
        {
            await _dataStore.InsertAsync(tx.GetHash(), tx);
            return tx.GetHash();
        }

        public async Task<Transaction> GetTransaction(Hash txId)
        {
            return await _dataStore.GetAsync<Transaction>(txId);
        }

        public async Task RemoveTransaction(Hash txId)
        {
            await _dataStore.RemoveAsync<Transaction>(txId);
        }
        
        public async Task<List<Transaction>> RollbackTransactions(Hash chainId, ulong currentHeight, ulong specificHeight)
        {
            var txs = new List<Transaction>();
            for (var i = currentHeight - 1; i >= specificHeight; i--)
            {
                var rollBackBlockHash =
                    await _dataStore.GetAsync<Hash>(
                        DataPath.CalculatePointerForGettingBlockHashByHeight(chainId, i));
                var header = await _dataStore.GetAsync<BlockHeader>(rollBackBlockHash);
                var body = await _dataStore.GetAsync<BlockBody>(
                    Hash.Xor(
                    header.GetHash(),header.MerkleTreeRootOfTransactions));
                foreach (var txId in body.Transactions)
                {
                    var tx = await _dataStore.GetAsync<Transaction>(txId);
                    if (tx == null)
                    {
                        _logger?.Trace($"tx {txId} is null.");
                    }
                    txs.Add(tx);
                    await _dataStore.RemoveAsync<Transaction>(txId);
                }

                _logger?.Trace($"Rollback block hash: {rollBackBlockHash.Value.ToByteArray().ToHex()}");
            }

            return txs;
        }
    }
}
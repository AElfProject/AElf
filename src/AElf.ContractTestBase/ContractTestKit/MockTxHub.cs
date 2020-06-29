using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;

namespace AElf.ContractTestBase.ContractTestKit 
{
    public class MockTxHub : ITxHub
    {
        private readonly IBlockchainService _blockchainService;

        private readonly Dictionary<Hash, Transaction> _allTransactions =
            new Dictionary<Hash, Transaction>();

        private long _bestChainHeight = AElfConstants.GenesisBlockHeight - 1;
        private Hash _bestChainHash = Hash.Empty;

        public MockTxHub(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        public Task<ExecutableTransactionSet> GetExecutableTransactionSetAsync(Hash blockHash, int transactionCount = 0)
        {
            return Task.FromResult(new ExecutableTransactionSet
            {
                PreviousBlockHash = _bestChainHash,
                PreviousBlockHeight = _bestChainHeight,
                Transactions = _allTransactions.Values.ToList()
            });
        }

        public async Task AddTransactionsAsync(IEnumerable<Transaction> transactions)
        {
            var txs = transactions.ToList();
            foreach (var transaction in txs)
            {
                _allTransactions.Add(transaction.GetHash(), transaction);
            }

            await _blockchainService.AddTransactionsAsync(txs);
        }

        public Task CleanByTransactionIdsAsync(IEnumerable<Hash> transactionIds)
        {
            CleanTransactions(transactionIds);
            
            return Task.CompletedTask;
        }

        public async Task UpdateTransactionPoolByBestChainAsync(Hash bestChainHash, long bestChainHeight)
        {
            _bestChainHeight = bestChainHeight;
            _bestChainHash = bestChainHash;
            await Task.CompletedTask;
        }

        public async Task CleanByHeightAsync(long height)
        {
            await Task.CompletedTask;
        }

        public Task<QueuedTransaction> GetQueuedTransactionAsync(Hash transactionId)
        {
            throw new System.NotImplementedException();
        }

        public Task<TransactionPoolStatus> GetTransactionPoolStatusAsync()
        {
            return Task.FromResult<TransactionPoolStatus>(new TransactionPoolStatus
            {
                AllTransactionCount = _allTransactions.Count,
            });
        }

        private void CleanTransactions(IEnumerable<Hash> transactionIds)
        {
            foreach (var transactionId in transactionIds)
            {
                _allTransactions.Remove(transactionId, out _);
            }
        }
    }
}
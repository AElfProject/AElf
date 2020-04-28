using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;

namespace AElf.Contracts.TestBase
{
    public class MockTxHub : ITxHub
    {
        private readonly ITransactionManager _transactionManager;
        private readonly IBlockchainService _blockchainService;

        private readonly Dictionary<Hash, Transaction> _allTransactions =
            new Dictionary<Hash, Transaction>();

        private long _bestChainHeight = AElfConstants.GenesisBlockHeight - 1;
        private Hash _bestChainHash = Hash.Empty;

        public MockTxHub(ITransactionManager transactionManager, IBlockchainService blockchainService)
        {
            _transactionManager = transactionManager;
            _blockchainService = blockchainService;
        }

        public async Task<ExecutableTransactionSet> GetExecutableTransactionSetAsync(int transactionCount = 0)
        {
            var executableTransactionSet = await Task.FromResult(new ExecutableTransactionSet
            {
                PreviousBlockHash = _bestChainHash,
                PreviousBlockHeight = _bestChainHeight,
                Transactions = _allTransactions.Values.ToList()
            });

            return executableTransactionSet;
        }

        public async Task AddTransactionsAsync(TransactionsReceivedEvent eventData)
        {
            foreach (var transaction in eventData.Transactions)
            {
                _allTransactions.Add(transaction.GetHash(), transaction);
                await _transactionManager.AddTransactionAsync(transaction);
            }
        }

        public Task HandleBlockAcceptedAsync(BlockAcceptedEvent eventData)
        {
            CleanTransactions(eventData.Block.Body.TransactionIds.ToList());
            
            return Task.CompletedTask;
        }

        public async Task HandleBestChainFoundAsync(BestChainFoundEventData eventData)
        {
            _bestChainHeight = eventData.BlockHeight;
            _bestChainHash = eventData.BlockHash;
            await Task.CompletedTask;
        }

        public async Task HandleNewIrreversibleBlockFoundAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            await Task.CompletedTask;
        }

        public async Task CleanTransactionsAsync(IEnumerable<Hash> transactions)
        {
            CleanTransactions(transactions);
            await Task.CompletedTask;
        }

        public Task<QueuedTransaction> GetQueuedTransactionAsync(Hash transactionId)
        {
            throw new System.NotImplementedException();
        }

        public Task<int> GetAllTransactionCountAsync()
        {
            return Task.FromResult(_allTransactions.Count);
        }

        public Task<int> GetValidatedTransactionCountAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> IsTransactionExistsAsync(Hash transactionId)
        {
            throw new System.NotImplementedException();
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
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    public class MockTxHub : ITxHub
    {
        private Dictionary<Hash, TransactionReceipt> _data = new Dictionary<Hash, TransactionReceipt>();

        public Task<ExecutableTransactionSet> GetExecutableTransactionSetAsync(int transactionCount = 0)
        {
            throw new System.NotImplementedException();
        }

        public Task HandleTransactionsReceivedAsync(TransactionsReceivedEvent eventData)
        {
            throw new System.NotImplementedException();
        }

        public Task HandleBlockAcceptedAsync(BlockAcceptedEvent eventData)
        {
            throw new System.NotImplementedException();
        }

        public Task HandleBestChainFoundAsync(BestChainFoundEventData eventData)
        {
            throw new System.NotImplementedException();
        }

        public Task HandleNewIrreversibleBlockFoundAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            throw new System.NotImplementedException();
        }

        public Task HandleUnexecutableTransactionsFoundAsync(UnexecutableTransactionsFoundEvent eventData)
        {
            throw new System.NotImplementedException();
        }

        public async Task<TransactionReceipt> GetTransactionReceiptAsync(Hash transactionId)
        {
            return await Task.FromResult(_data[transactionId]);
        }

        public Task<int> GetTransactionPoolSizeAsync()
        {
            throw new System.NotImplementedException();
        }

        public void AddTransaction(Transaction transaction)
        {
            _data[transaction.GetHash()] = new TransactionReceipt
            {
                TransactionId = transaction.GetHash(),
                Transaction = transaction
            };
        }
    }
}
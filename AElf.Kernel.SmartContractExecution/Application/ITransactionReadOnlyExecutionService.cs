using System;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface ITransactionReadOnlyExecutionService
    {
        Task<TransactionTrace> ExecuteAsync(IChainContext chainContext, Transaction transaction,
            DateTime currentBlockTime);
    }

    public class TransactionReadOnlyExecutionService : ITransactionReadOnlyExecutionService
    {
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;

        public TransactionReadOnlyExecutionService(ISmartContractExecutiveService smartContractExecutiveService)
        {
            _smartContractExecutiveService = smartContractExecutiveService;
        }

        public async Task<TransactionTrace> ExecuteAsync(IChainContext chainContext, Transaction transaction,
            DateTime currentBlockTime)
        {
            var trace = new TransactionTrace()
            {
                TransactionId = transaction.GetHash()
            };

            var transactionContext = new TransactionContext
            {
                PreviousBlockHash = chainContext.BlockHash,
                CurrentBlockTime = currentBlockTime,
                Transaction = transaction,
                BlockHeight = chainContext.BlockHeight + 1,
                Trace = trace,
                CallDepth = 0,
            };

            var executive = await _smartContractExecutiveService.GetExecutiveAsync(
                chainContext, transaction.To);

            try
            {
                executive.SetDataCache(chainContext.StateCache);
                await executive.SetTransactionContext(transactionContext).Apply();

            }
            finally
            {
                await _smartContractExecutiveService.PutExecutiveAsync(transaction.To, executive);
            }

            return trace;
        }
    }
}
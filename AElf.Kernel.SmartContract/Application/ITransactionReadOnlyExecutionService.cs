using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ITransactionReadOnlyExecutionService
    {
        Task<TransactionTrace> ExecuteAsync(IChainContext chainContext, Transaction transaction,
            DateTime currentBlockTime);
        Task<byte[]> GetFileDescriptorSetAsync(IChainContext chainContext, Address address);
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

        //TODO: Add test case GetFileDescriptorSetAsync [Case]
        public async Task<byte[]> GetFileDescriptorSetAsync(IChainContext chainContext, Address address)
        {
            IExecutive executive = null;

            byte[] output;
            try
            {
                executive = await _smartContractExecutiveService.GetExecutiveAsync(
                    chainContext, address);
                executive.SetDataCache(chainContext.StateCache);
                output = executive.GetFileDescriptorSet();
            }
            finally
            {
                if (executive != null)
                {
                    await _smartContractExecutiveService.PutExecutiveAsync(address, executive);    
                }
            }

            return output;
        }
    }
}
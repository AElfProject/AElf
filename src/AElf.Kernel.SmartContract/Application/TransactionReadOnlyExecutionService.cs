using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.Application
{
    public class TransactionReadOnlyExecutionService : ITransactionReadOnlyExecutionService
    {
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;

        public TransactionReadOnlyExecutionService(ISmartContractExecutiveService smartContractExecutiveService)
        {
            _smartContractExecutiveService = smartContractExecutiveService;
        }

        public async Task<TransactionTrace> ExecuteAsync(IChainContext chainContext, Transaction transaction,
            Timestamp currentBlockTime)
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
                StateCache = chainContext.StateCache
            };

            var executive = await _smartContractExecutiveService.GetExecutiveAsync(
                chainContext, transaction.To);

            try
            {
                await executive.ApplyAsync(transactionContext);
            }
            finally
            {
                await _smartContractExecutiveService.PutExecutiveAsync(transaction.To, executive);
            }

            return trace;
        }

        public async Task<byte[]> GetFileDescriptorSetAsync(IChainContext chainContext, Address address)
        {
            IExecutive executive = null;

            byte[] output;
            try
            {
                executive = await _smartContractExecutiveService.GetExecutiveAsync(
                    chainContext, address);
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

        public async Task<IEnumerable<FileDescriptor>> GetFileDescriptorsAsync(IChainContext chainContext, Address address)
        {
            IExecutive executive = null;

            IEnumerable<FileDescriptor> output;
            try
            {
                executive = await _smartContractExecutiveService.GetHistoryExecutiveAsync(
                    chainContext, address);
                output = executive.GetFileDescriptors();
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

        public async Task<string> GetTransactionParametersAsync(IChainContext chainContext, Transaction transaction)
        {
            var address = transaction.To;
            IExecutive executive = null;
            try
            {
                executive = await _smartContractExecutiveService.GetExecutiveAsync(chainContext, address);
                return executive.GetJsonStringOfParameters(transaction.MethodName, transaction.Params.ToByteArray());
            }
            finally
            {
                if (executive != null)
                {
                    await _smartContractExecutiveService.PutExecutiveAsync(address, executive);
                }
            }
        }
    }
}
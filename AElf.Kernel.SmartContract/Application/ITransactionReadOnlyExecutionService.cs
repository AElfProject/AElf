using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ITransactionReadOnlyExecutionService
    {
        Task<TransactionTrace> ExecuteAsync(IChainContext chainContext, Transaction transaction,
            DateTime currentBlockTime);

        Task<byte[]> GetFileDescriptorSetAsync(IChainContext chainContext, Address address);

        Task<string> GetTransactionParametersAsync(IChainContext chainContext, Transaction transaction);
    }

    public static class TransactionReadOnlyExecutionServiceExtensions
    {
        public static async Task<T> ExecuteAsync<T>(
            this ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            IChainContext chainContext, Transaction transaction,
            DateTime currentBlockTime, bool failedThrowException) where T : class, IMessage<T>, new()
        {
            var trace = await transactionReadOnlyExecutionService.ExecuteAsync(chainContext, transaction,
                currentBlockTime);
            if (trace.IsSuccessful())
            {
                var obj = new T();
                obj.MergeFrom(trace.ReturnValue);
                return obj;
            }

            if (failedThrowException)
            {
                throw new SmartContractExecutingException(trace.StdErr);
            }

            return default(T);
        }

        public static async Task<T> ExecuteAsync<T>(
            this ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            IChainContext chainContext, Transaction transaction,
            DateTime currentBlockTime) where T : class, IMessage<T>, new()
        {
            return await ExecuteAsync<T>(transactionReadOnlyExecutionService, chainContext, transaction,
                currentBlockTime,
                true);
        }
    }


    [Serializable]
    public class SmartContractExecutingException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public SmartContractExecutingException()
        {
        }

        public SmartContractExecutingException(string message) : base(message)
        {
        }

        public SmartContractExecutingException(string message, Exception inner) : base(message, inner)
        {
        }

        protected SmartContractExecutingException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
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
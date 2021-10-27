using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.Application
{
    public static class TransactionReadOnlyExecutionServiceExtensions
    {
        public static async Task<T> ExecuteAsync<T>(
            this ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            IChainContext chainContext, Transaction transaction,
            Timestamp currentBlockTime, bool failedThrowException) where T : class, IMessage<T>, new()
        {
            var trace = await transactionReadOnlyExecutionService.ExecuteAsync(chainContext, transaction, currentBlockTime);
            if (trace.IsSuccessful())
            {
                var obj = new T();
                obj.MergeFrom(trace.ReturnValue);
                return obj;
            }

            if (failedThrowException)
            {
                throw new SmartContractExecutingException(trace.Error);
            }

            return default(T);
        }
    }
}
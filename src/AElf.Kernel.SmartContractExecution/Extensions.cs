using System;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContractExecution
{
    public static class Extensions
    {
        public static async Task<ByteString> ExecuteTransactionAsync(
            this ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            IChainContext chainContext,
            Address from,
            Address to,
            string methodName,
            ByteString param)
        {
            var transaction = new Transaction()
            {
                From = from,
                To = to,
                MethodName = methodName,
                Params = param,
                Signature = ByteString.CopyFromUtf8("SignaturePlaceholder")
            };

            var transactionResult =
                await transactionReadOnlyExecutionService.ExecuteAsync(chainContext, transaction,
                    TimestampHelper.GetUtcNow());

            if (!transactionResult.IsSuccessful())
                throw new InvalidOperationException();

            return transactionResult.ReturnValue;
        }
    }
}
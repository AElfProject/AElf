using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acs2;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Kernel.SmartContractExecution;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.Parallel
{
    internal static class ExecutiveExtensions
    {
        // TODO: maybe use ITransactionReadOnlyExecutionService
        public static async Task<TransactionResourceInfo> GetTransactionResourceInfoAsync(this IExecutive executive,
            IChainContext chainContext, Transaction input)
        {
            var generatedTxn = new Transaction
            {
                From = Address.FromBytes(new byte[] { }.ComputeHash()),
                To = input.To,
                MethodName =
                    nameof(ACS2BaseContainer.ACS2BaseStub.GetResourceInfo),
                Params = input.ToByteString(),
                Signature = ByteString.CopyFromUtf8("SignaturePlaceholder")
            };
            var txId = input.GetHash();
            if (!IsParallelizable(executive))
            {
                return NotParallelizable(txId);
            }

            var trace = new TransactionTrace
            {
                TransactionId = generatedTxn.GetHash()
            };

            var transactionContext = new TransactionContext
            {
                PreviousBlockHash = chainContext.BlockHash,
                CurrentBlockTime = TimestampHelper.GetUtcNow(),
                Transaction = generatedTxn,
                BlockHeight = chainContext.BlockHeight + 1,
                Trace = trace,
                CallDepth = 0,
                StateCache = chainContext.StateCache
            };

            await executive.ApplyAsync(transactionContext);
            if (!trace.IsSuccessful())
            {
                return NotParallelizable(txId);
            }

            var resourceInfo = ResourceInfo.Parser.ParseFrom(trace.ReturnValue);
            return new TransactionResourceInfo
            {
                TransactionId = txId,
                Resources =
                {
                    resourceInfo.Reources
                },
                NonParallelizable = resourceInfo.NonParallelizable
            };
        }

        private static bool IsParallelizable(this IExecutive executive)
        {
            return executive.Descriptors.Any(service => service.File.GetIndentity() == "acs2");
        }

        private static TransactionResourceInfo NotParallelizable(Hash transactionId)
        {
            return new TransactionResourceInfo
            {
                TransactionId = transactionId,
                NonParallelizable = true
            };
        }
    }
}
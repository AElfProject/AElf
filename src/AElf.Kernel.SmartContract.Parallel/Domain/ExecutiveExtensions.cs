using System;
using System.Linq;
using System.Threading.Tasks;
using Acs2;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Parallel
{
    internal static class ExecutiveExtensions
    {
        private static Address FromAddress => Address.FromBytes(new byte[] { }.ComputeHash());

        public static async Task<TransactionResourceInfo> GetTransactionResourceInfoAsync(this IExecutive executive,
            IChainContext chainContext, Transaction input)
        {
            var txId = input.GetHash();
            var transactionContext = GetTransactionContext(chainContext, input.To, input.ToByteString(), out var trace);
            await executive.ApplyAsync(transactionContext);
            if (!trace.IsSuccessful())
            {
                return NotParallelizable(txId, executive.ContractHash);
            }

            try
            {
                var resourceInfo = ResourceInfo.Parser.ParseFrom(trace.ReturnValue);
                return new TransactionResourceInfo
                {
                    TransactionId = txId,
                    Paths =
                    {
                        resourceInfo.Paths
                    },
                    ParallelType = resourceInfo.NonParallelizable
                        ? ParallelType.NonParallelizable
                        : ParallelType.Parallelizable,
                    ContractHash = executive.ContractHash
                };
            }
            catch (Exception)
            {
                return NotParallelizable(txId, executive.ContractHash);
            }
        }

        internal static bool IsParallelizable(this IExecutive executive)
        {
            return executive.Descriptors.Any(service => service.File.GetIdentity() == "acs2");
        }

        private static TransactionResourceInfo NotParallelizable(Hash transactionId,Hash codeHash)
        {
            return new TransactionResourceInfo
            {
                TransactionId = transactionId,
                ParallelType = ParallelType.NonParallelizable,
                ContractHash = codeHash
            };
        }

        private static TransactionContext GetTransactionContext(IChainContext chainContext, Address to,
            ByteString param, out TransactionTrace trace)
        {
            var generatedTxn = new Transaction
            {
                From = FromAddress,
                To = to,
                MethodName = nameof(ACS2BaseContainer.ACS2BaseStub.GetResourceInfo),
                Params = param,
                Signature = ByteString.CopyFromUtf8(KernelConstants.SignaturePlaceholder)
            };

            trace = new TransactionTrace
            {
                TransactionId = generatedTxn.GetHash()
            };

            return new TransactionContext
            {
                PreviousBlockHash = chainContext.BlockHash,
                CurrentBlockTime = TimestampHelper.GetUtcNow(),
                Transaction = generatedTxn,
                BlockHeight = chainContext.BlockHeight + 1,
                Trace = trace,
                CallDepth = 0,
                StateCache = chainContext.StateCache
            };
        }
    }
}
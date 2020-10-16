using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS2;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel
{
    internal static class ExecutiveExtensions
    {
        public static async Task<TransactionResourceInfo> GetTransactionResourceInfoAsync(this IExecutive executive,
            ITransactionContext transactionContext, Hash txId)
        {
            await executive.ApplyAsync(transactionContext);
            if (!transactionContext.Trace.IsSuccessful())
            {
                return NotParallelizable(txId, executive.ContractHash);
            }

            try
            {
                var resourceInfo = ResourceInfo.Parser.ParseFrom(transactionContext.Trace.ReturnValue);
                return new TransactionResourceInfo
                {
                    TransactionId = txId,
                    WritePaths =
                    {
                        resourceInfo.WritePaths
                    },
                    ReadPaths = {resourceInfo.ReadPaths},
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
    }
}
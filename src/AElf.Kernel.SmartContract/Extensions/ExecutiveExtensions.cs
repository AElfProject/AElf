using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Extensions
{
    internal static class ExecutiveExtensions
    {
        public static async Task<SmartContractRegistration> GetSmartContractRegistrationFromZeroAsync(
            this IExecutive executiveZero, IChainContext chainContext, Transaction transaction)
        {
            var trace = new TransactionTrace
            {
                TransactionId = transaction.GetHash()
            };

            var txCtxt = new TransactionContext
            {
                PreviousBlockHash = chainContext.BlockHash,
                CurrentBlockTime = TimestampHelper.GetUtcNow(),
                Transaction = transaction,
                BlockHeight = chainContext.BlockHeight + 1,
                Trace = trace,
                CallDepth = 0,
                StateCache = chainContext.StateCache
            };

            await executiveZero.ApplyAsync(txCtxt);
            var returnBytes = txCtxt.Trace?.ReturnValue;
            if (returnBytes != null && returnBytes != ByteString.Empty)
            {
                return SmartContractRegistration.Parser.ParseFrom(returnBytes);
            }

            if (!txCtxt.Trace.IsSuccessful())
                throw new SmartContractFindRegistrationException(
                    $"failed to find registration from zero contract {txCtxt.Trace.Error}");
            return null;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;

internal partial class MethodFeeChargedPreExecutionPluginBase
{
    internal async Task<FlowBehavior> HandleExceptionWhileGettingPreTransactions(Exception ex,
        IReadOnlyList<ServiceDescriptor> descriptors, ITransactionContext transactionContext)
    {
        Logger.LogError(
            $"Failed to generate ChargeTransactionFees tx.Transaction to: {transactionContext.Transaction.To},transation method name: {transactionContext.Transaction.MethodName}. Error message: {ex.Message}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }
}
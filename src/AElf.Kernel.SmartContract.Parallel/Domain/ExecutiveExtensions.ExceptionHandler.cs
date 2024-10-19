using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel;

internal static partial class ExecutiveExtensions
{
    private async static Task<FlowBehavior> HandleExceptionWhileParsingResourceInfo(Exception ex, IExecutive executive,
        ITransactionContext transactionContext, Hash txId)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = NotParallelizable(txId, executive.ContractHash)
        };
    }
}
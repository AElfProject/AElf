using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.TransactionPool.Infrastructure;

public partial class TxHub
{
    protected async virtual Task<FlowBehavior> HandleExceptionWhileProcessingQueuedTransaction(Exception ex,
        QueuedTransaction queuedTransaction, Func<QueuedTransaction, Task<QueuedTransaction>> func)
    {
        Logger.LogError(ex,
            $"Unacceptable transaction {queuedTransaction.TransactionId}. Func: {func?.Method.Name}");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        };
    }
}
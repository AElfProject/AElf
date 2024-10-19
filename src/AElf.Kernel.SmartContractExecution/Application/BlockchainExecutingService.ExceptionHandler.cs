using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Kernel.Blockchain.Application;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContractExecution.Application;

public partial class FullBlockchainExecutingService
{
    protected async virtual Task<FlowBehavior> HandleExceptionWhileExecutingBlocks(BlockValidationException ex)
    {
        Logger.LogDebug(
            $"Block validation failed: {ex.Message}. Inner exception {ex.InnerException.Message}");
        if (ex.InnerException is not ValidateNextTimeBlockValidationException)
        {
            return new FlowBehavior
            {
                ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
            };
        }

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue
        };
    }
}
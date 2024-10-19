using System;
using System.IO;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace AElf.WebApp.Application.Chain;

public partial class TransactionAppService
{
    protected async Task<FlowBehavior> HandleExceptionWhileExecutingTransactionAsReadOnly(Exception ex)
    {
        using var detail = new StringReader(ex.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                Error.InvalidTransaction.ToString(), await detail.ReadLineAsync())
        };
    }
    
    protected async Task<FlowBehavior> HandleExceptionWhileEstimatingTransactionFee(Exception ex)
    {
        using var detail = new StringReader(ex.Message);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                Error.InvalidTransaction.ToString(), await detail.ReadLineAsync())
        };
    }
    
    protected async Task<FlowBehavior> HandleExceptionWhileParsingExecutionResult(Exception ex, byte[] response, Transaction transaction)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = response?.ToHex()
        };
    }

    protected async Task<FlowBehavior> HandleExceptionWhileParsingTransaction(Exception ex, int errorCode = Error.InvalidParams)
    {
        Logger.LogError(ex, "{ErrorMessage}", ex.Message); //for debug
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new UserFriendlyException(Error.Message[Error.InvalidParams],
                Error.InvalidParams.ToString())
        };
    }
    
    protected async Task<FlowBehavior> HandleExceptionWhileValidatingMessage(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }
    
    protected async Task<FlowBehavior> HandleExceptionWhileParsingTransactionParameter(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new UserFriendlyException(Error.Message[Error.InvalidParams], Error.InvalidParams.ToString())
        };
    }
}
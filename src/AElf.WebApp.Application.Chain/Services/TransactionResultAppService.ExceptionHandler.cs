using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace AElf.WebApp.Application.Chain;

public partial class TransactionResultAppService
{
    protected async Task<FlowBehavior> HandleExceptionWhileParsingTransactionParameter(Exception ex,
        TransactionDto transaction, ByteString @params)
    {
        Logger.LogError(ex, "Failed to parse transaction params: {params}", transaction.Params);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }
}
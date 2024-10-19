using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Volo.Abp;

namespace AElf.WebApp.Application.Chain;

public class ExceptionHandlerService
{
    public async Task<FlowBehavior> HandleExceptionWhileParsingHash(Exception ex, string hexHash, int errorCode)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new UserFriendlyException(Error.Message[errorCode],
                errorCode.ToString())
        };
    }
}
using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace AElf.WebApp.Application.Chain;

public partial class ContractFileDescriptorSetAppService
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileGettingContractFileDescriptorSet(Exception ex)
    {
        Logger.LogWarning(ex, "Error during GetContractFileDescriptorSetAsync");

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString())
        };
    }
}
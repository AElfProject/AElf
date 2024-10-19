using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;

namespace AElf.ContractTestKit.AEDPoSExtension;

public partial class BlockMiningService
{
    protected async Task<FlowBehavior> HandleExceptionWhileGettingProperContractStub(Exception ex)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new BlockMiningException("Failed to find proper contract stub.", ex)
        };
    }
}
using System;
using AElf.ExceptionHandler;

namespace AElf.Kernel.Blockchain.Domain;

public partial class BlockManager
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileGettingBlock(
        Exception ex, Hash blockHash)
    {
        Logger.LogError(ex, "Error while getting block {BlockHash}", blockHash.ToHex());
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = null
        };
    }
}
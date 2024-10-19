using System;
using AElf.ExceptionHandler;

namespace AElf.Kernel.SmartContract.Application;

public partial class BlockchainStateService
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileMergingBlockState(Exception ex,
        ChainStateInfo chainStateInfo, IBlockIndex blockIndex)
    {
        Logger.LogError(ex,
            "Exception while merge state {ChainStateInfo} for block {BlockIndex}", chainStateInfo, blockIndex);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }
}
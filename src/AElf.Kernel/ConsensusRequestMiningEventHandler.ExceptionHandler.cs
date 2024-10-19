using System;
using AElf.ExceptionHandler;
using AElf.Kernel.Consensus;

namespace AElf.Kernel;

public partial class ConsensusRequestMiningEventHandler
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileRequestingMining(Exception ex, ConsensusRequestMiningEventData eventData,
        Chain chain)
    {
        await TriggerConsensusEventAsync(chain.BestChainHash, chain.BestChainHeight);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }
}
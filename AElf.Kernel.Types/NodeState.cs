namespace AElf.Kernel.Types
{
    public enum NodeState
    {
        /// <summary>
        /// Catching (other) BPs, can't mining.
        /// Available next state:
        /// HeaderValidating,
        /// GeneratingConsensusTx
        /// </summary>
        Catching,
        
        /// <summary>
        /// Already mined at least one block.
        /// Available next state:
        /// HeaderValidating,
        /// GeneratingConsensusTx
        /// </summary>
        Caught,
        
        /// <summary>
        /// Add this block to valid block cache if success. Actually we use this to maintain valid block cache.
        /// Maybe separate from other states.
        /// Available next state:
        /// BlockValidating,
        /// GeneratingConsensusTx,
        /// Reverting
        /// </summary>
        HeaderValidating,
        
        /// <summary>
        /// Execute this block if success.
        /// Available next state:
        /// BlockExecuting,
        /// GeneratingConsensusTx
        /// </summary>
        BlockValidating,
        
        /// <summary>
        /// Executing block, can be cancelled.
        /// Available next state:
        /// BlockAppending,
        /// Reverting
        /// </summary>
        BlockExecuting,
        
        /// <summary>
        /// Executing block, can't be cancelled.
        /// Available next state:
        /// HeaderValidating,
        /// GeneratingConsensusTx
        /// </summary>
        BlockAppending,
        
        /// <summary>
        /// Maybe waiting for side chain information.
        /// Available next state:
        /// HeaderValidating,
        /// GeneratingConsensusTx
        /// </summary>
        ExecutingLoop,
        
        /// <summary>   
        /// Mining, can be cancelled.
        /// Available next state:
        /// ProducingBlock,
        /// BlockExecuting
        /// </summary>
        GeneratingConsensusTx,
        
        /// <summary>
        /// Mining, can't be cancelled.
        /// Available next state:
        /// BlockValidating,
        /// Reverting
        /// </summary>
        ProducingBlock,
        
        /// <summary>
        /// Doing rollback, can't do anything.
        /// Available next state:
        /// Caught
        /// </summary>
        Reverting
    }

    public static class NodeStateExtensions
    {
        public static bool AsMiner(this NodeState nodeState)
        {
            return (int) nodeState > 0;
        }
    }
}
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
        /// BlockExecuting2,
        /// Reverting
        /// </summary>
        BlockExecuting1,
        
        /// <summary>
        /// Executing block, can't be cancelled.
        /// Available next state:
        /// HeaderValidating
        /// </summary>
        BlockExecuting2,
        
        /// <summary>
        /// Mining, can be cancelled.
        /// Available next state:
        /// ProducingBlock,
        /// BlockExecuting1
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
        /// Catching
        /// </summary>
        Reverting
    }
}
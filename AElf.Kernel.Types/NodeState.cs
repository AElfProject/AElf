namespace AElf.Kernel.Types
{
    public enum NodeState
    {
        /// <summary>
        /// Catching (other) BPs, can't mining.
        /// Available next state:
        /// BlockValidating - (ValidBlockHeader)
        /// GeneratingConsensusTx - (MiningStart)
        /// </summary>
        Catching,
        
        /// <summary>
        /// Already mined at least one block.
        /// Available next state:
        /// BlockValidating - (ValidBlockHeader)
        /// GeneratingConsensusTx - (MiningStart)
        /// </summary>
        Caught,
        
        /// <summary>
        /// Execute this block if success.
        /// Available next state:
        /// BlockExecuting - (ValidBlock)
        /// GeneratingConsensusTx - (MiningStart)
        /// </summary>
        BlockValidating,
        
        /// <summary>
        /// Executing block, can be cancelled.
        /// Available next state:
        /// BlockAppending - 
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
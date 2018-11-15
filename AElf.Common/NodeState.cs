namespace AElf.Common
{
    public enum NodeState
    {
        /// <summary>
        /// Catching (other) BPs, can't mining.
        /// Available next state:
        /// BlockValidating - (ValidBlockHeader)
        /// GeneratingConsensusTx - (MiningStart)
        /// </summary>
        Catching = 1,
        
        /// <summary>
        /// Already mined at least one block.
        /// Available next state:
        /// BlockValidating - (ValidBlockHeader)
        /// GeneratingConsensusTx - (MiningStart)
        /// Reverting - (ForkDetected)
        /// </summary>
        Caught,
        
        /// <summary>
        /// Execute this block if success.
        /// Can't mining during this process.
        /// Available next state:
        /// BlockExecuting - (ValidBlock)
        /// GeneratingConsensusTx - (MiningStart)
        /// Catching / Caught - (ForkDetected & _caught)
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
        ExecutingLoop = 10,
        
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
        Reverting,
        
        Stay
    }

    public static class NodeStateExtensions
    {
        public static bool ShouldLockMiningWhenEntering(this NodeState nodeState)
        {
            return (int) nodeState < 10 || nodeState == NodeState.Reverting;
        }
        
        public static bool ShouldUnlockMiningWhenLeaving(this NodeState nodeState)
        {
            return (int) nodeState < 10;
        }
    }
}
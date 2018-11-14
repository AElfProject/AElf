using AElf.Kernel.Types;

namespace AElf.Synchronization.EventMessages
{
    public sealed class LeavingState
    {
        public NodeState NodeState { get; }

        public LeavingState(NodeState nodeState)
        {
            NodeState = nodeState;
        }
    }
}
using AElf.Kernel.Types;

namespace AElf.Synchronization.EventMessages
{
    public sealed class EnteringState
    {
        public NodeState NodeState { get; }

        public EnteringState(NodeState nodeState)
        {
            NodeState = nodeState;
        }
    }
}
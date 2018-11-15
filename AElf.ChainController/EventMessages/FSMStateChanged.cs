using AElf.Common;

namespace AElf.ChainController.EventMessages
{
    // ReSharper disable once InconsistentNaming
    public sealed class FSMStateChanged
    {
        public NodeState CurrentState { get; }

        public FSMStateChanged(NodeState currentState)
        {
            CurrentState = currentState;
        }
    }
}
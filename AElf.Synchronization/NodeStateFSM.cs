using AElf.ChainController.EventMessages;
using AElf.Common.FSM;
using AElf.Kernel.Types;
using AElf.Synchronization.EventMessages;
using Easy.MessageHub;
using NLog;

namespace AElf.Synchronization
{
    // ReSharper disable InconsistentNaming
    public class NodeStateFSM
    {
        private readonly ILogger _logger = LogManager.GetLogger(nameof(NodeStateFSM));

        private FSM<NodeState> _fsm;

        private bool _caught;

        public FSM<NodeState> Create()
        {
            _fsm = new FSM<NodeState>();
            AddStates();
            return _fsm;
        }

        private void AddStates()
        {
            AddCatching();
            AddCaught();
            AddBlockValidating();
            AddBlockExecuting();
            AddBlockAppending();
            AddGeneratingConsensusTx();
            AddProducingBlock();
            AddExecutingLoop();
            AddReverting();
        }

        private void AddCatching()
        {
            NodeState TransferFromCatching()
            {
                if (_fsm.StateEvent == StateEvent.ValidBlockHeader)
                {
                    return NodeState.BlockValidating;
                }

                if (_fsm.StateEvent == StateEvent.MiningStart)
                {
                    _caught = true;
                    return NodeState.GeneratingConsensusTx;
                }

                UnexpectedStateEvent(_fsm.StateEvent);
                return NodeState.Catching;
            }

            _fsm.AddState(NodeState.Catching)
                .SetTransferFunction(TransferFromCatching)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddCaught()
        {
            NodeState TransferFromCaught()
            {
                if (_fsm.StateEvent == StateEvent.ValidBlockHeader)
                {
                    return NodeState.BlockValidating;
                }

                if (_fsm.StateEvent == StateEvent.MiningStart)
                {
                    return NodeState.GeneratingConsensusTx;
                }
                
                if (_fsm.StateEvent == StateEvent.ForkDetected)
                {
                    return NodeState.Reverting;
                }

                UnexpectedStateEvent(_fsm.StateEvent);
                return NodeState.Caught;
            }

            _fsm.AddState(NodeState.Caught)
                .SetTransferFunction(TransferFromCaught)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddBlockValidating()
        {
            NodeState TransferFromBlockValidating()
            {
                if (_fsm.StateEvent == StateEvent.ValidBlock)
                {
                    return NodeState.BlockExecuting;
                }

                if (_fsm.StateEvent == StateEvent.InvalidBlock)
                {
                    return _caught ? NodeState.Caught : NodeState.Catching;
                }
                
                if (_fsm.StateEvent == StateEvent.ForkDetected && _caught)
                {
                    return NodeState.Reverting;
                }

                UnexpectedStateEvent(_fsm.StateEvent);
                return NodeState.BlockValidating;
            }

            _fsm.AddState(NodeState.BlockValidating)
                .SetTransferFunction(TransferFromBlockValidating)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddBlockExecuting()
        {
            NodeState TransferFromBlockExecuting()
            {
                if (_fsm.StateEvent == StateEvent.StateUpdated)
                {
                    return NodeState.BlockAppending;
                }

                if (_fsm.StateEvent == StateEvent.StateNotUpdated)
                {
                    return NodeState.ExecutingLoop;
                }
                
                if (_fsm.StateEvent == StateEvent.ForkDetected && _caught)
                {
                    return NodeState.Reverting;
                }

                UnexpectedStateEvent(_fsm.StateEvent);
                return NodeState.BlockExecuting;
            }

            _fsm.AddState(NodeState.BlockExecuting)
                .SetTransferFunction(TransferFromBlockExecuting)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddBlockAppending()
        {
            NodeState TransferFromAddBlockAppending()
            {
                if (_fsm.StateEvent == StateEvent.BlockAppended)
                {
                    return _caught ? NodeState.Caught : NodeState.Catching;
                }

                UnexpectedStateEvent(_fsm.StateEvent);
                return NodeState.BlockAppending;
            }

            _fsm.AddState(NodeState.BlockAppending)
                .SetTransferFunction(TransferFromAddBlockAppending)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddGeneratingConsensusTx()
        {
            NodeState TransferFromAddGeneratingConsensusTx()
            {
                if (_fsm.StateEvent == StateEvent.ConsensusTxGenerated)
                {
                    return NodeState.ProducingBlock;
                }
                
                if (_fsm.StateEvent == StateEvent.ForkDetected)
                {
                    return NodeState.Reverting;
                }

                if (_fsm.StateEvent == StateEvent.MiningEnd)
                {
                    return NodeState.Caught;
                }

                UnexpectedStateEvent(_fsm.StateEvent);
                return NodeState.GeneratingConsensusTx;
            }

            _fsm.AddState(NodeState.GeneratingConsensusTx)
                .SetTransferFunction(TransferFromAddGeneratingConsensusTx)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddProducingBlock()
        {
            NodeState TransferFromAddProducingBlock()
            {
                if (_fsm.StateEvent == StateEvent.MiningEnd)
                {
                    return NodeState.Caught;
                }

                UnexpectedStateEvent(_fsm.StateEvent);
                return NodeState.ProducingBlock;
            }

            _fsm.AddState(NodeState.ProducingBlock)
                .SetTransferFunction(TransferFromAddProducingBlock)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddExecutingLoop()
        {
            NodeState TransferFromAddExecutingLoop()
            {
                if (_fsm.StateEvent == StateEvent.ValidBlockHeader)
                {
                    return NodeState.ExecutingLoop;
                }

                if (_fsm.StateEvent == StateEvent.StateUpdated)
                {
                    return NodeState.BlockAppending;
                }

                if (_fsm.StateEvent == StateEvent.MiningStart)
                {
                    return NodeState.GeneratingConsensusTx;
                }
                
                if (_fsm.StateEvent == StateEvent.ForkDetected && _caught)
                {
                    return NodeState.Reverting;
                }

                UnexpectedStateEvent(_fsm.StateEvent);
                return NodeState.ExecutingLoop;
            }

            _fsm.AddState(NodeState.ExecutingLoop)
                .SetTransferFunction(TransferFromAddExecutingLoop)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddReverting()
        {
            NodeState TransferFromAddReverting()
            {
                if (_fsm.StateEvent == StateEvent.RollbackFinished)
                {
                    _caught = false;
                    return NodeState.Catching;
                }

                UnexpectedStateEvent(_fsm.StateEvent);
                return NodeState.ProducingBlock;
            }

            _fsm.AddState(NodeState.Reverting)
                .SetTransferFunction(TransferFromAddReverting)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void WhenEnteringState()
        {
            _logger?.Trace($"[NodeState] Entering State {_fsm.CurrentState.ToString()}");
            MessageHub.Instance.Publish(new EnteringState(_fsm.CurrentState));
        }

        private void WhenLeavingState()
        {
            _logger?.Trace($"[NodeState] Leaving State {_fsm.CurrentState.ToString()}");
            MessageHub.Instance.Publish(new LeavingState(_fsm.CurrentState));
        }

        private void UnexpectedStateEvent(StateEvent @event)
        {
            if (_fsm.CurrentState.ShouldLockMiningWhenEntering())
            {
                MessageHub.Instance.Publish(new LockMining(false));
            }
            
            _logger?.Trace(
                $"Unexpected state event {@event.ToString()}. Current state is {_fsm.CurrentState.ToString()}");
        }
    }
}
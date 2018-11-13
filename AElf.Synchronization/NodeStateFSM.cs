using AElf.Common.FSM;
using AElf.Kernel.Types;
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

                if (_fsm.StateEvent == StateEvent.ForkDetected)
                {
                    return NodeState.Reverting;
                }

                UnexpectedLog(_fsm.StateEvent);
                return NodeState.Catching;
            }

            _fsm.AddState(NodeState.Catching)
                .SetTransferFunction(TransferFromCatching)
                .OnEntering(LogWhenEntering)
                .OnLeaving(LogWhenLeaving);
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

                UnexpectedLog(_fsm.StateEvent);
                return NodeState.Caught;
            }

            _fsm.AddState(NodeState.Caught)
                .SetTransferFunction(TransferFromCaught)
                .OnEntering(LogWhenEntering)
                .OnLeaving(LogWhenLeaving);
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
                
                if (_fsm.StateEvent == StateEvent.ForkDetected)
                {
                    return NodeState.Reverting;
                }

                UnexpectedLog(_fsm.StateEvent);
                return NodeState.BlockValidating;
            }

            _fsm.AddState(NodeState.BlockValidating)
                .SetTransferFunction(TransferFromBlockValidating)
                .OnEntering(LogWhenEntering)
                .OnLeaving(LogWhenLeaving);
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
                
                if (_fsm.StateEvent == StateEvent.ForkDetected)
                {
                    return NodeState.Reverting;
                }

                UnexpectedLog(_fsm.StateEvent);
                return NodeState.BlockExecuting;
            }

            _fsm.AddState(NodeState.BlockExecuting)
                .SetTransferFunction(TransferFromBlockExecuting)
                .OnEntering(LogWhenEntering)
                .OnLeaving(LogWhenLeaving);
        }

        private void AddBlockAppending()
        {
            NodeState TransferFromAddBlockAppending()
            {
                if (_fsm.StateEvent == StateEvent.BlockAppended)
                {
                    return _caught ? NodeState.Caught : NodeState.Catching;
                }

                UnexpectedLog(_fsm.StateEvent);
                return NodeState.BlockAppending;
            }

            _fsm.AddState(NodeState.BlockAppending)
                .SetTransferFunction(TransferFromAddBlockAppending)
                .OnEntering(LogWhenEntering)
                .OnLeaving(LogWhenLeaving);
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

                UnexpectedLog(_fsm.StateEvent);
                return NodeState.GeneratingConsensusTx;
            }

            _fsm.AddState(NodeState.GeneratingConsensusTx)
                .SetTransferFunction(TransferFromAddGeneratingConsensusTx)
                .OnEntering(LogWhenEntering)
                .OnLeaving(LogWhenLeaving);
        }

        private void AddProducingBlock()
        {
            NodeState TransferFromAddProducingBlock()
            {
                if (_fsm.StateEvent == StateEvent.MiningEnd)
                {
                    return NodeState.Caught;
                }

                UnexpectedLog(_fsm.StateEvent);
                return NodeState.ProducingBlock;
            }

            _fsm.AddState(NodeState.ProducingBlock)
                .SetTransferFunction(TransferFromAddProducingBlock)
                .OnEntering(LogWhenEntering)
                .OnLeaving(LogWhenLeaving);
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
                
                if (_fsm.StateEvent == StateEvent.ForkDetected)
                {
                    return NodeState.Reverting;
                }

                UnexpectedLog(_fsm.StateEvent);
                return NodeState.ExecutingLoop;
            }

            _fsm.AddState(NodeState.ExecutingLoop)
                .SetTransferFunction(TransferFromAddExecutingLoop)
                .OnEntering(LogWhenEntering)
                .OnLeaving(LogWhenLeaving);
        }

        private void AddReverting()
        {
            NodeState TransferFromAddReverting()
            {
                if (_fsm.StateEvent == StateEvent.RollbackFinished)
                {
                    return NodeState.Catching;
                }

                UnexpectedLog(_fsm.StateEvent);
                return NodeState.ProducingBlock;
            }

            _fsm.AddState(NodeState.Reverting)
                .SetTransferFunction(TransferFromAddReverting)
                .OnEntering(LogWhenEntering)
                .OnLeaving(LogWhenLeaving);
        }

        private void LogWhenEntering()
        {
            _logger?.Trace($"Entering State {_fsm.CurrentState.ToString()}");
        }

        private void LogWhenLeaving()
        {
            _logger?.Trace($"Leaving State {_fsm.CurrentState.ToString()}");
        }

        private void UnexpectedLog(StateEvent @event)
        {
            _logger?.Trace(
                $"Unexpected state event {@event.ToString()}. Current state is {_fsm.CurrentState.ToString()}");
        }
    }
}
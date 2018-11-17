using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Common.FSM;
using AElf.Synchronization.EventMessages;
using Easy.MessageHub;
using NLog;

namespace AElf.Synchronization
{
    // ReSharper disable InconsistentNaming
    public class NodeStateFSM
    {
        private readonly ILogger _logger = LogManager.GetLogger(nameof(NodeStateFSM));

        private FSM _fsm;

        private bool _caught;

        public FSM Create()
        {
            _fsm = new FSM();
            AddStates();
            _fsm.CurrentState = (int) NodeState.Catching;
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
            int TransferFromCatching()
            {
                if (_fsm.StateEvent == StateEvent.ValidBlockHeader)
                {
                    return (int) NodeState.BlockValidating;
                }

                if (_fsm.StateEvent == StateEvent.MiningStart)
                {
                    _caught = true;
                    return (int) NodeState.GeneratingConsensusTx;
                }

                return (int) NodeState.Stay;
            }

            _fsm.AddState((int) NodeState.Catching)
                .SetTransferFunction(TransferFromCatching)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddCaught()
        {
            int TransferFromCaught()
            {
                if (_fsm.StateEvent == StateEvent.ValidBlockHeader)
                {
                    return (int) NodeState.BlockValidating;
                }

                if (_fsm.StateEvent == StateEvent.MiningStart)
                {
                    return (int) NodeState.GeneratingConsensusTx;
                }

                if (_fsm.StateEvent == StateEvent.LongerChainDetected)
                {
                    return (int) NodeState.Reverting;
                }

                return (int) NodeState.Stay;
            }

            _fsm.AddState((int) NodeState.Caught)
                .SetTransferFunction(TransferFromCaught)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddBlockValidating()
        {
            int TransferFromBlockValidating()
            {
                if (_fsm.StateEvent == StateEvent.ValidBlock)
                {
                    return (int) NodeState.BlockExecuting;
                }

                if (_fsm.StateEvent == StateEvent.InvalidBlock)
                {
                    return (int) (_caught ? NodeState.Caught : NodeState.Catching);
                }

                if (_fsm.StateEvent == StateEvent.LongerChainDetected && _caught)
                {
                    return (int) NodeState.Reverting;
                }

                return (int) NodeState.Stay;
            }

            _fsm.AddState((int) NodeState.BlockValidating)
                .SetTransferFunction(TransferFromBlockValidating)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddBlockExecuting()
        {
            int TransferFromBlockExecuting()
            {
                if (_fsm.StateEvent == StateEvent.StateUpdated)
                {
                    return (int) NodeState.BlockAppending;
                }

                if (_fsm.StateEvent == StateEvent.StateNotUpdated)
                {
                    return (int) NodeState.ExecutingLoop;
                }

                if (_fsm.StateEvent == StateEvent.LongerChainDetected)
                {
                    if (_caught)
                    {
                        return (int) NodeState.Reverting;
                    }
                }

                if (_fsm.StateEvent == StateEvent.BlockAppended)
                {
                    return (int) (_caught ? NodeState.Caught : NodeState.Catching);
                }

                return (int) NodeState.Stay;
            }

            _fsm.AddState((int) NodeState.BlockExecuting)
                .SetTransferFunction(TransferFromBlockExecuting)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddBlockAppending()
        {
            int TransferFromAddBlockAppending()
            {
                if (_fsm.StateEvent == StateEvent.BlockAppended)
                {
                    return (int) (_caught ? NodeState.Caught : NodeState.Catching);
                }

                return (int) NodeState.Stay;
            }

            _fsm.AddState((int) NodeState.BlockAppending)
                .SetTransferFunction(TransferFromAddBlockAppending)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddGeneratingConsensusTx()
        {
            int TransferFromAddGeneratingConsensusTx()
            {
                if (_fsm.StateEvent == StateEvent.ConsensusTxGenerated)
                {
                    return (int) NodeState.ProducingBlock;
                }

                if (_fsm.StateEvent == StateEvent.LongerChainDetected)
                {
                    return (int) NodeState.Reverting;
                }

                if (_fsm.StateEvent == StateEvent.MiningEnd)
                {
                    return (int) NodeState.Caught;
                }

                return (int) NodeState.Stay;
            }

            _fsm.AddState((int) NodeState.GeneratingConsensusTx)
                .SetTransferFunction(TransferFromAddGeneratingConsensusTx)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddProducingBlock()
        {
            int TransferFromAddProducingBlock()
            {
                if (_fsm.StateEvent == StateEvent.MiningEnd)
                {
                    return (int) NodeState.Caught;
                }

                return (int) NodeState.Stay;
            }

            _fsm.AddState((int) NodeState.ProducingBlock)
                .SetTransferFunction(TransferFromAddProducingBlock)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddExecutingLoop()
        {
            int TransferFromAddExecutingLoop()
            {
                if (_fsm.StateEvent == StateEvent.ValidBlockHeader)
                {
                    return (int) NodeState.ExecutingLoop;
                }

                if (_fsm.StateEvent == StateEvent.StateUpdated)
                {
                    return (int) NodeState.BlockAppending;
                }

                if (_fsm.StateEvent == StateEvent.MiningStart)
                {
                    return (int) NodeState.GeneratingConsensusTx;
                }

                if (_fsm.StateEvent == StateEvent.LongerChainDetected && _caught)
                {
                    return (int) NodeState.Reverting;
                }

                return (int) NodeState.Stay;
            }

            _fsm.AddState((int) NodeState.ExecutingLoop)
                .SetTransferFunction(TransferFromAddExecutingLoop)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void AddReverting()
        {
            int TransferFromAddReverting()
            {
                if (_fsm.StateEvent == StateEvent.RollbackFinished)
                {
                    _caught = false;
                    return (int) NodeState.Catching;
                }

                return (int) NodeState.Stay;
            }

            _fsm.AddState((int) NodeState.Reverting)
                .SetTransferFunction(TransferFromAddReverting)
                .OnEntering(WhenEnteringState)
                .OnLeaving(WhenLeavingState);
        }

        private void WhenEnteringState()
        {
            _logger?.Trace($"[NodeState] Entering State {((NodeState) _fsm.CurrentState).ToString()}");
            MessageHub.Instance.Publish(new EnteringState((NodeState) _fsm.CurrentState));
        }

        private void WhenLeavingState()
        {
            _logger?.Trace($"[NodeState] Leaving State {((NodeState) _fsm.CurrentState).ToString()}");
            MessageHub.Instance.Publish(new LeavingState((NodeState) _fsm.CurrentState));
        }
    }
}
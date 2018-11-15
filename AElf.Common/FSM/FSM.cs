using System;
using System.Collections.Generic;
using NLog;

namespace AElf.Common.FSM
{
    // ReSharper disable InconsistentNaming
    public class FSM
    {
        private readonly Dictionary<int, FSMStateBehaviour> _states = new Dictionary<int, FSMStateBehaviour>();

        private int _currentState;

        public int CurrentState
        {
            get => _currentState;
            set
            {
                _currentStateBehaviour?.Leaving();

                _stateAge = -1000;
                _currentStateBehaviour = _states[value];
                _currentState = value;
                
                _currentStateBehaviour?.Entering();
            }
        }

        public StateEvent StateEvent { get; set; }

        private FSMStateBehaviour _currentStateBehaviour;

        private int _stateAge = -1000;

        private readonly ILogger _logger = LogManager.GetLogger(nameof(FSM));

        public FSMStateBehaviour AddState(int state)
        {
            var behaviour = new FSMStateBehaviour(state);
            _states.Add(state, behaviour);
            return behaviour;
        }

        public void ProcessWithNumber(int time)
        {
            // Initial state age for current state.
            _stateAge = _stateAge < 0 ? time : _stateAge;
            
            var total = time;
            var stateTime = total - _stateAge;
            var progress = 0;

            if (_currentStateBehaviour.Duration.HasValue)
            {
                progress = (int) Math.Max(0, Math.Min(1000, stateTime / _currentStateBehaviour.Duration.Value * 1000));
            }
            
            var data = new FSMStateData
            {
                FSM = this,
                StateBehaviour = _currentStateBehaviour,
                CurrentState = _currentState,
                Age = stateTime,
                AbsTime = total,
                Progress = progress
            };

            _currentStateBehaviour.Invoke(data);

            if (progress >= 1000 && _currentStateBehaviour.StateTransferFunction != null)
            {
                CurrentState = _currentStateBehaviour.StateTransferFunction();
                _stateAge = time;
            }
        }

        public void ProcessWithStateEvent(StateEvent stateEvent)
        {
            _logger?.Trace($"[StateEvent] {stateEvent.ToString()}");
            StateEvent = stateEvent;

            if ((NodeState) CurrentState == NodeState.Stay)
            {
                return;
            }
            
            if (_currentStateBehaviour.StateTransferFunction != null)
            {
                var nextState = _currentStateBehaviour.StateTransferFunction();
                if (CurrentState != nextState)
                {
                    CurrentState = nextState;
                }
            }
        }

        public void NextState()
        {
            if (_currentStateBehaviour != null)
            {
                CurrentState = _currentStateBehaviour.StateTransferFunction();
            }
        }
    }
}
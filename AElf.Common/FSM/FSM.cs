using System;
using System.Collections.Generic;

namespace AElf.Common.FSM
{
    // ReSharper disable InconsistentNaming
    public class FSM<T>
    {
        private Dictionary<T, FSMStateBehaviour<T>> _states = new Dictionary<T, FSMStateBehaviour<T>>();

        private T _currentState;

        public T CurrentState
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

        private FSMStateBehaviour<T> _currentStateBehaviour;

        private double _stateAge = -1000;

        public FSMStateBehaviour<T> AddState(T state)
        {
            var behaviour = new FSMStateBehaviour<T>(state);
            _states.Add(state, behaviour);
            return behaviour;
        }

        public void Process(double time)
        {
            _stateAge = _stateAge < 0 ? time : _stateAge;
            var total = time;
            var stateTime = total - _stateAge;
            var progress = 0d;

            if (_currentStateBehaviour.Duration.HasValue)
            {
                progress = Math.Max(0, Math.Min(1000, stateTime / _currentStateBehaviour.Duration.Value * 1000));
            }
            
            var data = new FSMState<T>
            {
                FSM = this,
                StateBehaviour = _currentStateBehaviour,
                CurrentState = _currentState,
                Age = stateTime,
                AbsTime = total,
                Progress = progress
            };

            _currentStateBehaviour.Invoke(data);

            if (progress >= 1000 && _currentStateBehaviour.NextStateSelector != null)
            {
                CurrentState = _currentStateBehaviour.NextStateSelector();
                _stateAge = time;
            }
        }

        public void NextState()
        {
            if (_currentStateBehaviour != null)
            {
                CurrentState = _currentStateBehaviour.NextStateSelector();
            }
        }
    }
}
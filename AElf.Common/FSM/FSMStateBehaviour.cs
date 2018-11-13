using System;
using System.Collections.Generic;

namespace AElf.Common.FSM
{
    // ReSharper disable once InconsistentNaming
    public class FSMStateBehaviour<T>
    {
        private readonly List<Action<FSMStateData<T>>> _processCallbacks = new List<Action<FSMStateData<T>>>();
        private readonly List<Action> _enterCallbackList = new List<Action>();
        private readonly List<Action> _leaveCallbackList = new List<Action>();

        public T State { get; set; }
        public double? Duration { get; set; }
        public Func<T> StateTransferFunction { get; set; }
        
        public FSMStateBehaviour(T state)
        {
            State = state;
        }

        public FSMStateBehaviour<T> OnEntering(Action callback)
        {
            _enterCallbackList.Add(callback);
            return this;
        }
        
        public FSMStateBehaviour<T> OnLeaving(Action callback)
        {
            _leaveCallbackList.Add(callback);
            return this;
        }

        public FSMStateBehaviour<T> AddCallback(Action<FSMStateData<T>> callback)
        {
            _processCallbacks.Add(callback);
            return this;
        }
        
        public FSMStateBehaviour<T> SetTimeout(double duration)
        {
            Duration = duration;
            return this;
        }

        public FSMStateBehaviour<T> TransferTo(Func<T> stateTransferFunction)
        {
            StateTransferFunction = stateTransferFunction;
            return this;
        }

        public void Invoke(FSMStateData<T> data)
        {
            foreach (var callback in _processCallbacks)
            {
                callback(data);
            }
        }

        public void Entering()
        {
            foreach (var callback in _enterCallbackList)
            {
                callback();
            }
        }
        
        public void Leaving()
        {
            foreach (var callback in _leaveCallbackList)
            {
                callback();
            }
        }
    }
}
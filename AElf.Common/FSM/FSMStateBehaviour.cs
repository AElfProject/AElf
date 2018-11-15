using System;
using System.Collections.Generic;

namespace AElf.Common.FSM
{
    // ReSharper disable once InconsistentNaming
    public class FSMStateBehaviour
    {
        private readonly List<Action<FSMStateData>> _processCallbacks = new List<Action<FSMStateData>>();
        private readonly List<Action> _enterCallbackList = new List<Action>();
        private readonly List<Action> _leaveCallbackList = new List<Action>();

        public int State { get; set; }
        public double? Duration { get; set; }
        public Func<int> StateTransferFunction { get; set; }

        public FSMStateBehaviour(int state)
        {
            State = state;
        }

        public FSMStateBehaviour OnEntering(Action callback)
        {
            _enterCallbackList.Add(callback);
            return this;
        }

        public FSMStateBehaviour OnLeaving(Action callback)
        {
            _leaveCallbackList.Add(callback);
            return this;
        }

        public FSMStateBehaviour AddCallback(Action<FSMStateData> callback)
        {
            _processCallbacks.Add(callback);
            return this;
        }

        public FSMStateBehaviour SetTimeout(double duration)
        {
            Duration = duration;
            return this;
        }

        public FSMStateBehaviour SetTransferFunction(Func<int> stateTransferFunction)
        {
            StateTransferFunction = stateTransferFunction;
            return this;
        }

        public void Invoke(FSMStateData data)
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
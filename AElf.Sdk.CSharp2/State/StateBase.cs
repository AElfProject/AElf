using System;
using System.Collections.Generic;
using AElf.Kernel;
using AElf.Kernel.Managers;

namespace AElf.Sdk.CSharp.State
{
    public class StateBase
    {
        private IStateManager _stateManager;
        private StatePath _path;
        private IContextInternal _context;

        internal IStateManager Manager
        {
            get => _stateManager;
            set
            {
                _stateManager = value;
                OnStateManagerSet();
            }
        }

        internal StatePath Path
        {
            get => _path;
            set
            {
                _path = value;
                OnPathSet();
            }
        }

        internal IContextInternal Context
        {
            get => _context;
            set
            {
                _context = value;
                OnContextSet();
            }
        }

        internal virtual void OnStateManagerSet()
        {
        }

        internal virtual void OnPathSet()
        {
        }

        internal virtual void OnContextSet()
        {
        }

        internal virtual void Clear()
        {
        }

        internal virtual Dictionary<StatePath, StateValue> GetChanges()
        {
            return new Dictionary<StatePath, StateValue>();
        }
    }
}
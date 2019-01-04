using System;
using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Sdk.CSharp.State
{
    public class StateBase
    {
        private StateContext _context;
        private StatePath _path;

        internal StateContext Context
        {
            get => _context;
            set
            {
                _context = value;
                OnContextSet();
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

        internal virtual void OnContextSet()
        {
        }

        internal virtual void OnPathSet()
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
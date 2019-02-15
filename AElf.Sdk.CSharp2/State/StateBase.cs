using System;
using System.Collections.Generic;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Contexts;

namespace AElf.Sdk.CSharp.State
{
    public class StateBase
    {
        private IStateProvider _provider;
        private StatePath _path;
        private IContextInternal _context;

        internal IStateProvider Provider
        {
            get => _provider;
            set
            {
                _provider = value;
                OnProviderSet();
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

        internal virtual void OnProviderSet()
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
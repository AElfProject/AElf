using System;
using System.Collections.Generic;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractBridge;

namespace AElf.Sdk.CSharp.State
{
    public class StateBase
    {
        private IStateProvider _provider;
        private StatePath _path;
        private ISmartContractBridgeContext _context;

        internal IStateProvider Provider
        {
            get => _provider;
            set
            {
                _provider = value;
                OnProviderSet();
            }
        }

        internal virtual StatePath Path
        {
            get => _path;
            set
            {
                _path = value;
                OnPathSet();
            }
        }

        internal ISmartContractBridgeContext Context
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

        internal virtual TransactionExecutingStateSet GetChanges()
        {
            return new TransactionExecutingStateSet();
        }
    }
}
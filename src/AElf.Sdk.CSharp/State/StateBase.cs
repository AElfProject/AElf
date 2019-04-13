using System;
using System.Collections.Generic;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Sdk.CSharp.State
{
    public class StateBase
    {
        private StatePath _path;
        private CSharpSmartContractContext _context;
        internal IStateProvider Provider => _context.StateProvider;

        internal StatePath Path
        {
            get => _path;
            set
            {
                _path = value;
                OnPathSet();
            }
        }

        internal CSharpSmartContractContext Context
        {
            get => _context;
            set
            {
                _context = value;
                OnContextSet();
            }
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
using AElf.Kernel;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Sdk.CSharp.State
{
    public class StateBase
    {
        private ISmartContractBridgeContext _context;
        private StatePath _path;
        private IStateProvider _provider;

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
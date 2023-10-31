using AElf.Kernel.SmartContract;
using AElf.Types;

namespace AElf.Sdk.CSharp.State;

public class StateBase
{
    private ISmartContractBridgeContext _context;
    private StatePath _path;
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

    internal ISmartContractBridgeContext Context
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
using AElf.Kernel.SmartContract;
using AElf.Types;

namespace AElf.Sdk.CSharp.State;

public class StateBase
{
    private ISmartContractBridgeContext _context;
    private StatePath _path;
    internal IStateProvider Provider => _context.StateProvider;

    public StatePath Path
    {
        get => _path;
        set
        {
            _path = value;
            OnPathSet();
        }
    }

    public ISmartContractBridgeContext Context
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

    public virtual void Clear()
    {
    }

    public virtual TransactionExecutingStateSet GetChanges()
    {
        return new TransactionExecutingStateSet();
    }
}
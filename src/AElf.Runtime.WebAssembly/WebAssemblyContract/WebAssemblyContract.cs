using AElf.Kernel.SmartContract;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Runtime.WebAssembly;

public class WebAssemblyContract : ISmartContract
{
}

public class WebAssemblyContract<TContractState> where TContractState : ContractState, new()
{
    public TContractState State { get; internal set; } = new()
    {
        Path = new StatePath()
    };

    public WebAssemblySmartContractContext Context { get; set; }

    internal TransactionExecutingStateSet GetChanges()
    {
        return State.GetChanges();
    }

    internal void Cleanup()
    {
        State.Clear();
    }

    internal void InternalInitialize(ISmartContractBridgeContext bridgeContext)
    {
        if (Context != null)
            throw new InvalidOperationException();
        Context = new WebAssemblySmartContractContext(bridgeContext);
        State.Context = Context;
        OnInitialized();
    }

    protected virtual void OnInitialized()
    {
    }
}
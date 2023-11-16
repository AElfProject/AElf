using AElf.Kernel.SmartContract;
using AElf.Runtime.WebAssembly.TransactionPayment;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Runtime.WebAssembly.Contract;

public class WebAssemblyContract : ISmartContract
{
}

public class WebAssemblyContract<TContractState> where TContractState : ContractState, new()
{
    public TContractState State { get; internal set; } = new()
    {
        Path = new StatePath()
    };

    public IGasMeter? GasMeter { get; set; }

    public List<string> RuntimeLogs = new();
    public List<string> CustomPrints = new();
    public List<string> ErrorMessages = new();
    public List<string> DebugMessages = new();
    public List<(byte[], byte[])> Events = new();

    public WebAssemblySmartSmartContractContext Context { get; set; }

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
        Context = new WebAssemblySmartSmartContractContext(bridgeContext);
        State.Context = Context;
        OnInitialized();
    }

    internal IGasMeter GetGasMeter()
    {
        return GasMeter!;
    }

    internal List<string> GetRuntimeLogs()
    {
        return RuntimeLogs;
    }

    internal List<string> GetCustomPrints()
    {
        return CustomPrints;
    }

    internal List<string> GetDebugMessages()
    {
        return DebugMessages;
    }

    internal List<string> GetErrorMessages()
    {
        return ErrorMessages;
    }

    internal List<(byte[], byte[])> GetEvents()
    {
        return Events;
    }

    protected virtual void OnInitialized()
    {
    }
}
using System.Numerics;
using AElf.Kernel.SmartContract;
using AElf.Runtime.WebAssembly.TransactionPayment;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using LanguageExt;

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

    public List<string> RuntimeLogs = new();
    public List<string> CustomPrints = new();
    public List<string> ErrorMessages = new();
    public List<string> DebugMessages = new();
    public List<(byte[], byte[])> Events = new();
    public byte[]? InputData;
    public bool AlreadyTransferred;
    public bool AllowReentry = true;
    public long FuelLimit;
    public long ConsumedFuel;

    public WebAssemblySmartSmartContractContext Context { get; set; }

    internal TransactionExecutingStateSet GetChanges()
    {
        return State.GetChanges();
    }

    internal void Cleanup()
    {
        State.Clear();
        RuntimeLogs.Clear();
        CustomPrints.Clear();
        ErrorMessages.Clear();
        DebugMessages.Clear();
        Events.Clear();
        AlreadyTransferred = false;
    }

    internal void InternalInitialize(ISmartContractBridgeContext bridgeContext)
    {
        if (Context != null)
            throw new InvalidOperationException();
        Context = new WebAssemblySmartSmartContractContext(bridgeContext);
        State.Context = Context;
        OnInitialized();
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

    internal long GetConsumedFuel()
    {
        return ConsumedFuel;
    }

    internal List<(byte[], byte[])> GetEvents()
    {
        return Events;
    }

    protected virtual void OnInitialized()
    {
    }
}
using System.Text.Json;
using AElf.CSharp.CodeOps;
using AElf.Kernel;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Runtime.CSharp;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using NBitcoin.DataEncoders;
using Solang;
using Solang.Extensions;
using Wasmtime;

namespace AElf.Runtime.WebAssembly;

public class Executive : IExecutive
{
    public IReadOnlyList<ServiceDescriptor> Descriptors { get; }
    public Hash ContractHash { get; set; }
    public Timestamp LastUsedTime { get; set; }
    public string ContractVersion { get; set; }

    private readonly SolangABI _solangAbi;
    private readonly WebAssemblyContract _webAssemblyContract;
    private readonly CSharpSmartContractProxy _smartContractProxy;

    private IHostSmartContractBridgeContext _hostSmartContractBridgeContext;
    private ITransactionContext CurrentTransactionContext => _hostSmartContractBridgeContext.TransactionContext;

    public Executive(CompiledContract compiledContract)
    {
        var wasmCode = compiledContract.WasmCode.ToByteArray();
        _solangAbi = JsonSerializer.Deserialize<SolangABI>(compiledContract.Abi)!;
        ContractHash = HashHelper.ComputeFrom(wasmCode);
        _webAssemblyContract = new WebAssemblyContract(wasmCode);
        _smartContractProxy =
            new CSharpSmartContractProxy(_webAssemblyContract, typeof(ExecutionObserverProxy));
        // TODO: Maybe we are able to know the solidity code version.
        ContractVersion = "Unknown solidity version.";
    }

    public IExecutive SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext)
    {
        _hostSmartContractBridgeContext = smartContractBridgeContext;
        _smartContractProxy.InternalInitialize(_hostSmartContractBridgeContext);
        return this;
    }

    public Task ApplyAsync(ITransactionContext transactionContext)
    {
        try
        {
            _hostSmartContractBridgeContext.TransactionContext = transactionContext;
            if (CurrentTransactionContext.CallDepth > CurrentTransactionContext.MaxCallDepth)
            {
                CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ExceededMaxCallDepth;
                CurrentTransactionContext.Trace.Error = "\n" + "ExceededMaxCallDepth";
                return Task.CompletedTask;
            }

            Execute();
        }
        finally
        {
            _hostSmartContractBridgeContext.TransactionContext = null;
        }

        return Task.CompletedTask;
    }

    private void Execute()
    {
        var s = CurrentTransactionContext.Trace.StartTime = TimestampHelper.GetUtcNow().ToDateTime();
        var methodName = CurrentTransactionContext.Transaction.MethodName;

        var transactionContext = _hostSmartContractBridgeContext.TransactionContext;
        var transaction = transactionContext.Transaction;

        var isCallConstructor = transaction.MethodName == "deploy";

        if (isCallConstructor && _hostSmartContractBridgeContext.Origin != transaction.To)
        {
            transactionContext.Trace.ExecutionStatus = ExecutionStatus.Prefailed;
            transactionContext.Trace.Error = "Cannot execute constructor.";
            return;
        }

        var selector = transaction.MethodName == "deploy"
            ? _solangAbi.GetSelector(transaction.MethodName)
            : transaction.MethodName;
        var parameter = transaction.Params.ToHex();
        _webAssemblyContract.Input = Encoders.Hex.DecodeData(selector + parameter);
        var instance = _webAssemblyContract.Instantiate();
        var actionName = isCallConstructor ? "deploy" : "call";
        var action = instance.GetFunction<ActionResult>(actionName);
        if (action is null)
        {
            throw new WebAssemblyRuntimeException($"error: {actionName} export is missing");
        }

        var invokeResult = new RuntimeActionInvoker().Invoke(action);
        if (!invokeResult.Success)
        {
            _webAssemblyContract.DebugMessages.Add(invokeResult.DebugMessage);
        }

        if (_webAssemblyContract.DebugMessages.Count > 0)
        {
            transactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
            transactionContext.Trace.Error = _webAssemblyContract.DebugMessages.First();
        }
        else
        {
            transactionContext.Trace.ReturnValue = ByteString.CopyFrom(_webAssemblyContract.ReturnBuffer);
            transactionContext.Trace.ExecutionStatus = ExecutionStatus.Executed;
            foreach (var depositedEvent in _webAssemblyContract.Events)
            {
                transactionContext.Trace.Logs.Add(new LogEvent
                {
                    Address = transaction.To,
                    Name = depositedEvent.Item1.ToHex(),
                    NonIndexed = ByteString.CopyFrom(depositedEvent.Item2)
                });
            }
        }

        CurrentTransactionContext.Trace.StateSet = GetChanges();
        var e = CurrentTransactionContext.Trace.EndTime = TimestampHelper.GetUtcNow().ToDateTime();
        CurrentTransactionContext.Trace.Elapsed = (e - s).Ticks;
    }

    private TransactionExecutingStateSet GetChanges()
    {
        var changes = _smartContractProxy.GetChanges();

        var address = _hostSmartContractBridgeContext.Self.ToStorageKey();
        foreach (var key in changes.Writes.Keys)
            if (!key.StartsWith(address))
                throw new InvalidOperationException("a contract cannot access other contracts data");

        foreach (var (key, value) in changes.Deletes)
            if (!key.StartsWith(address))
                throw new InvalidOperationException("a contract cannot access other contracts data");

        foreach (var key in changes.Reads.Keys)
            if (!key.StartsWith(address))
                throw new InvalidOperationException("a contract cannot access other contracts data");

        if (!CurrentTransactionContext.Trace.IsSuccessful())
        {
            changes.Writes.Clear();
            changes.Deletes.Clear();
        }

        return changes;
    }

    public string GetJsonStringOfParameters(string methodName, byte[] paramsBytes)
    {
        throw new NotImplementedException();
    }

    public bool IsView(string methodName)
    {
        throw new NotImplementedException();
    }

    public byte[] GetFileDescriptorSet()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<FileDescriptor> GetFileDescriptors()
    {
        throw new NotImplementedException();
    }
}
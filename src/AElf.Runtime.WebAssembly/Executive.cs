using System.Text;
using System.Text.Json;
using AElf.Kernel;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Runtime.WebAssembly.Contract;
using AElf.Runtime.WebAssembly.TransactionPayment;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using NBitcoin.DataEncoders;
using Nethereum.Hex.HexConvertors.Extensions;
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
    private readonly WebAssemblyContractImplementation _webAssemblyContract;
    private readonly WebAssemblySmartContractProxy _smartContractProxy;

    private IHostSmartContractBridgeContext _hostSmartContractBridgeContext;
    private ITransactionContext CurrentTransactionContext => _hostSmartContractBridgeContext.TransactionContext;

    public Executive(string solangAbi)
    {
        _solangAbi = JsonSerializer.Deserialize<SolangABI>(solangAbi)!;
        ContractHash = Hash.LoadFromHex(_solangAbi.Source.Hash);
        var wasmCode = _solangAbi.Source.Wasm.HexToByteArray();
        _webAssemblyContract = new WebAssemblyContractImplementation(wasmCode);
        _smartContractProxy = new WebAssemblySmartContractProxy(_webAssemblyContract);

        ContractVersion = _solangAbi.Version;
        Descriptors = new List<ServiceDescriptor>();
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
        var startTime = CurrentTransactionContext.Trace.StartTime = TimestampHelper.GetUtcNow().ToDateTime();
        var methodName = CurrentTransactionContext.Transaction.MethodName;

        try
        {
            var transactionContext = _hostSmartContractBridgeContext.TransactionContext;
            var transaction = transactionContext.Transaction;

            var isCallConstructor = methodName == "deploy" || _solangAbi.GetConstructor() == methodName;

            if (isCallConstructor && _webAssemblyContract.Initialized)
            {
                transactionContext.Trace.ExecutionStatus = ExecutionStatus.Prefailed;
                transactionContext.Trace.Error = "Cannot execute constructor.";
                return;
            }

            var selector = isCallConstructor ? _solangAbi.GetConstructor() : methodName;
            string parameter;
            var value = 0L;
            var delegateCallValue = 0L;
            if (isCallConstructor)
            {
                parameter = transaction.Params.ToHex();
            }
            else
            {
                var solidityTransactionParameter = new SolidityTransactionParameter();
                solidityTransactionParameter.MergeFrom(transaction.Params);
                parameter = solidityTransactionParameter.Parameter.ToHex();
                value = solidityTransactionParameter.Value;
                delegateCallValue = solidityTransactionParameter.DelegateCallValue;

                if (solidityTransactionParameter.GasLimit is { RefTime: 0, ProofSize: 0 } )
                {
                    _webAssemblyContract.EstimateGas = true;
                }

                _webAssemblyContract.GasMeter = new GasMeter(solidityTransactionParameter.GasLimit);
            }

            var action = GetAction(selector, parameter, isCallConstructor, value, delegateCallValue);
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
                var events = _smartContractProxy.GetEvents();
                if (events != null)
                {
                    foreach (var depositedEvent in events)
                    {
                        var logEvent = new LogEvent
                        {
                            Address = transaction.To,
                            Name = Encoding.UTF8.GetString(depositedEvent.Item1),
                            NonIndexed = ByteString.CopyFrom(depositedEvent.Item2)
                        };
                        transactionContext.Trace.Logs.Add(logEvent);
                    }
                }
            }

            if (!isCallConstructor)
            {
                var gasMeter = _smartContractProxy.GetGasMeter();
                var logEvent = new LogEvent
                {
                    Address = transaction.To,
                    Name = _webAssemblyContract.EstimateGas
                        ? WebAssemblyTransactionPaymentConstants.GasFeeEstimatedLogEventName
                        : WebAssemblyTransactionPaymentConstants.GasFeeConsumedLogEventName,
                    NonIndexed = gasMeter.GasLeft.ToByteString()
                };
                CurrentTransactionContext.Trace.Logs.Add(logEvent);
            }

            CurrentTransactionContext.Trace.StateSet = GetChanges();
        }
        catch (Exception ex)
        {
            CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.SystemError;
            CurrentTransactionContext.Trace.Error += ex + "\n";
        }

        var endTime = CurrentTransactionContext.Trace.EndTime = TimestampHelper.GetUtcNow().ToDateTime();
        CurrentTransactionContext.Trace.Elapsed = (endTime - startTime).Ticks;

        ForDebug();
    }

    private void ForDebug()
    {
        var runtimeLogs = _smartContractProxy.GetRuntimeLogs();
        var prints = _smartContractProxy.GetCustomPrints();
        var errors = _smartContractProxy.GetErrorMessages();
        var debugs = _smartContractProxy.GetDebugMessages();
        var events = _smartContractProxy.GetEvents();
    }

    private Func<ActionResult> GetAction(string selector, string parameter, bool isCallConstructor, long value,
        long delegateCallValue)
    {
        var inputData = Encoders.Hex.DecodeData(selector + parameter);
        _webAssemblyContract.Value = value;
        _webAssemblyContract.DelegateCallValue = delegateCallValue;
        var instance = _webAssemblyContract.Instantiate(inputData);
        var actionName = isCallConstructor ? "deploy" : "call";
        var action = instance.GetFunction<ActionResult>(actionName);
        if (action is null)
        {
            throw new WebAssemblyRuntimeException($"error: {actionName} export is missing");
        }

        return action;
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

        if (CurrentTransactionContext.Trace.CallStateSet != null)
        {
            changes = changes.Merge(CurrentTransactionContext.Trace.CallStateSet);
        }
        if (CurrentTransactionContext.Trace.DelegateCallStateSet != null)
        {
            changes = changes.Merge(CurrentTransactionContext.Trace.DelegateCallStateSet.ReplaceAddress(address));
        }

        if (!CurrentTransactionContext.Trace.IsSuccessful())
        {
            changes.Writes.Clear();
            changes.Deletes.Clear();
        }

        return changes;
    }

    public string GetJsonStringOfParameters(string methodName, byte[] paramsBytes)
    {
        return string.Empty;
    }

    public bool IsView(string methodName)
    {
        return false;
    }

    public byte[] GetFileDescriptorSet()
    {
        return Array.Empty<byte>();
    }

    public IEnumerable<FileDescriptor> GetFileDescriptors()
    {
        return new List<FileDescriptor>();
    }
}
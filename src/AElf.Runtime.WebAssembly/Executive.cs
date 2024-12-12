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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly string _solangAbiContent;
    private readonly WebAssemblyContractImplementation _webAssemblyContract;
    private readonly WebAssemblySmartContractProxy _smartContractProxy;

    private IHostSmartContractBridgeContext _hostSmartContractBridgeContext;
    private ITransactionContext CurrentTransactionContext => _hostSmartContractBridgeContext.TransactionContext;

    public ILogger Logger { get; set; }

    public Executive(string solangAbi)
    {
        _solangAbiContent = solangAbi;
        _solangAbi = JsonSerializer.Deserialize<SolangABI>(solangAbi)!;
        ContractHash = Hash.LoadFromHex(_solangAbi.Source.Hash);
        var wasmCode = _solangAbi.Source.Wasm.HexToByteArray();
        _webAssemblyContract = new WebAssemblyContractImplementation(wasmCode, true);
        _smartContractProxy = new WebAssemblySmartContractProxy(_webAssemblyContract);

        ContractVersion = _solangAbi.Version;
        Descriptors = new List<ServiceDescriptor>();
        
        Logger = NullLogger<Executive>.Instance;
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

            if (methodName == "is_allow_reentry")
            {
                var reentrySelector = StringValue.Parser.ParseFrom(transaction.Params.ToByteArray()).Value;
                var mutates = _solangAbi.GetMutates(reentrySelector);
                transactionContext.Trace.ReturnValue =
                    new BoolValue
                    {
                        Value = !mutates || _webAssemblyContract.AllowReentry
                    }.ToByteString();
                transactionContext.Trace.ExecutionStatus = ExecutionStatus.Executed;
                return;
            }

            var selector = isCallConstructor ? _solangAbi.GetConstructor() : methodName;
            string parameter;
            var value = 0L;
            var delegateCallValue = 0L;
            var gasLimit = 0L;
            var estimatingGas = false;
            if (isCallConstructor)
            {
                parameter = transaction.Params.ToHex();
                // Method fee already charged by executing deploy method.
                _webAssemblyContract.IsChargeGas = false;
            }
            else
            {
                var solidityTransactionParameter = new SolidityTransactionParameter();
                solidityTransactionParameter.MergeFrom(transaction.Params);
                parameter = solidityTransactionParameter.Parameter.ToHex();
                value = solidityTransactionParameter.Value;
                delegateCallValue = solidityTransactionParameter.DelegateCallValue;

                if (solidityTransactionParameter.EstimateGas)
                {
                    estimatingGas = true;
                    _webAssemblyContract.IsChargeGas = false;
                }

                _webAssemblyContract.FuelLimit = solidityTransactionParameter.GasLimit;
                gasLimit = _webAssemblyContract.FuelLimit;
            }

            var action = GetAction(selector, parameter, isCallConstructor, value, delegateCallValue, gasLimit);
            var invokeResult = new RuntimeActionInvoker().Invoke(action);

            if (!invokeResult.Success)
            {
                _webAssemblyContract.DebugMessages.Add(invokeResult.DebugMessage);
            }

            if (_webAssemblyContract.DebugMessages.Count > 0)
            {
                var debugMessages = _smartContractProxy.GetDebugMessages();
                if (debugMessages != null)
                {
                    foreach (var debugMessage in debugMessages)
                    {
                        var logEvent = new LogEvent
                        {
                            Address = transaction.To,
                            Name = "DebugMessage",
                            NonIndexed = ByteString.CopyFrom(Encoding.UTF8.GetBytes(debugMessage.Trim()))
                        };
                        transactionContext.Trace.Logs.Add(logEvent);
                    }
                }
                transactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                transactionContext.Trace.Error = _webAssemblyContract.DebugMessages.First();
            }
            if (_webAssemblyContract.ErrorMessages.Count > 0)
            {
                var errorMessages = _smartContractProxy.GetErrorMessages();
                if (errorMessages != null)
                {
                    foreach (var errorMessage in errorMessages)
                    {
                        var logEvent = new LogEvent
                        {
                            Address = transaction.To,
                            Name = "ErrorMessage",
                            NonIndexed = ByteString.CopyFrom(Encoding.UTF8.GetBytes(errorMessage.Trim()))
                        };
                        transactionContext.Trace.Logs.Add(logEvent);
                    }
                }

                transactionContext.Trace.ExecutionStatus = ExecutionStatus.Canceled;
                transactionContext.Trace.Error = _webAssemblyContract.ErrorMessages.First();
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

                var prints = _smartContractProxy.GetCustomPrints();
                if (prints != null)
                {
                    foreach (var print in prints)
                    {
                        var logEvent = new LogEvent
                        {
                            Address = transaction.To,
                            Name = "Print",
                            NonIndexed = ByteString.CopyFrom(Encoding.UTF8.GetBytes(print.Trim()))
                        };
                        transactionContext.Trace.Logs.Add(logEvent);
                    }
                }
                
                var runtimeLogs = _smartContractProxy.GetRuntimeLogs();
                if (runtimeLogs != null)
                {
                    foreach (var runtimeLog in runtimeLogs)
                    {
                        var logEvent = new LogEvent
                        {
                            Address = transaction.To,
                            Name = "RuntimeLog",
                            NonIndexed = ByteString.CopyFrom(Encoding.UTF8.GetBytes(runtimeLog.Trim()))
                        };
                        transactionContext.Trace.Logs.Add(logEvent);
                    }
                }
            }

            if (!isCallConstructor)
            {
                var consumedFuel = _smartContractProxy.GetConsumedFuel() ?? 0;
                var logEvent = new LogEvent
                {
                    Address = transaction.To,
                    Name = _webAssemblyContract.IsChargeGas
                        ? WebAssemblyTransactionPaymentConstants.GasFeeEstimatedLogEventName
                        : WebAssemblyTransactionPaymentConstants.GasFeeChargedLogEventName,
                    NonIndexed = new Int64Value { Value = consumedFuel }.ToByteString()
                };
                CurrentTransactionContext.Trace.Logs.Add(logEvent);
            }

            if (!estimatingGas)
            {
                CurrentTransactionContext.Trace.StateSet = GetChanges();
            }
        }
        catch (Exception ex)
        {
            CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.SystemError;
            CurrentTransactionContext.Trace.Error += ex + "\n";
        }
        finally
        {
            Cleanup();
        }

        var endTime = CurrentTransactionContext.Trace.EndTime = TimestampHelper.GetUtcNow().ToDateTime();
        CurrentTransactionContext.Trace.Elapsed = (endTime - startTime).Ticks;
    }
    
    private void Cleanup()
    {
        _smartContractProxy.Cleanup();
    }

    private Func<ActionResult> GetAction(string selector, string parameter, bool isCallConstructor, long value,
        long delegateCallValue, long fuelLimit)
    {
        var inputData = Encoders.Hex.DecodeData(selector + parameter);
        _webAssemblyContract.Value = value;
        _webAssemblyContract.DelegateCallValue = delegateCallValue;
        var instance = _webAssemblyContract.Instantiate(inputData, fuelLimit);
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
            changes = changes.Merge(CurrentTransactionContext.Trace.DelegateCallStateSet);
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
        // var selector = _solangAbi.GetSelector(methodName);
        // return !_solangAbi.GetMutates(selector);
    }

    public byte[] GetFileDescriptorSet()
    {
        return _solangAbiContent.GetBytes();
    }

    public IEnumerable<FileDescriptor> GetFileDescriptors()
    {
        return new List<FileDescriptor>();
    }
}
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Runtime.WebAssembly.Extensions;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using NBitcoin.DataEncoders;
using Wasmtime;

namespace AElf.Runtime.WebAssembly;

public class Executive : IExecutive
{
    public IReadOnlyList<ServiceDescriptor> Descriptors { get; }
    public Hash ContractHash { get; set; }
    public Timestamp LastUsedTime { get; set; }
    public string ContractVersion { get; set; }

    private readonly WebAssemblyRuntime _webAssemblyRuntime;

    private IHostSmartContractBridgeContext _hostSmartContractBridgeContext;
    private ITransactionContext CurrentTransactionContext => _hostSmartContractBridgeContext.TransactionContext;

    public Executive(IExternalEnvironment ext, byte[] wasmCode)
    {
        _webAssemblyRuntime = new WebAssemblyRuntime(ext, wasmCode);
    }

    public IExecutive SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext)
    {
        _hostSmartContractBridgeContext = smartContractBridgeContext;
        _webAssemblyRuntime.SetHostSmartContractBridgeContext(smartContractBridgeContext);
        return this;
    }

    public Task ApplyAsync(ITransactionContext transactionContext)
    {
        _hostSmartContractBridgeContext.TransactionContext = transactionContext;
        Execute();
        return Task.CompletedTask;
    }

    private void Execute()
    {
        var s = CurrentTransactionContext.Trace.StartTime = TimestampHelper.GetUtcNow().ToDateTime();

        var transactionContext = _hostSmartContractBridgeContext.TransactionContext;
        var transaction = transactionContext.Transaction;
        if (transaction.MethodName == "deploy")
        {
            // TODO: Call deploy after deployment.
            var deploy = _webAssemblyRuntime.Instantiate().GetAction(transaction.MethodName);
            if (deploy is null)
            {
                throw new WebAssemblyRuntimeException("error: deploy export is missing");
            }
            InvokeAction(deploy);
            transactionContext.Trace.ExecutionStatus = ExecutionStatus.Executed;
            transactionContext.Trace.ReturnValue = ByteString.CopyFrom(_webAssemblyRuntime.ReturnBuffer);
            return;
        }

        var selector = transaction.MethodName;
        var parameters = transaction.Params.ToHex();
        _webAssemblyRuntime.Input = Encoders.Hex.DecodeData(selector + parameters);
        var instance = _webAssemblyRuntime.Instantiate();
        var call = instance.GetAction("call");
        if (call is null)
        {
            throw new WebAssemblyRuntimeException("error: call export is missing");
        }

        InvokeAction(call);
        transactionContext.Trace.ReturnValue = ByteString.CopyFrom(_webAssemblyRuntime.ReturnBuffer);
        transactionContext.Trace.ExecutionStatus = ExecutionStatus.Executed;

        var changes = _webAssemblyRuntime.GetChanges();
        CurrentTransactionContext.Trace.StateSet = changes;

        var e = CurrentTransactionContext.Trace.EndTime = TimestampHelper.GetUtcNow().ToDateTime();
        CurrentTransactionContext.Trace.Elapsed = (e - s).Ticks;
    }

    private void InvokeAction(Action? action)
    {
        try
        {
            action?.Invoke();
        }
        catch (TrapException ex)
        {
            if (ex.Message.Contains("wasm `unreachable` instruction executed"))
            {
                // Ignored.
            }
            else
            {
                Console.WriteLine("got exception " + ex.Message);
            }
        }
    }

    private void InvokeFunc(Func<int> function)
    {
        try
        {
            function?.Invoke();
        }
        catch (TrapException ex)
        {
            if (ex.Message.Contains("wasm `unreachable` instruction executed"))
            {
                // Ignored.
            }
            else
            {
                Console.WriteLine("got exception " + ex.Message);
            }
        }
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
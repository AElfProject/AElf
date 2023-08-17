using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using NBitcoin.DataEncoders;
using Nethereum.ABI;
using Wasmtime;

namespace AElf.Runtime.WebAssembly;

public class Executive : IExecutive
{
    public IReadOnlyList<ServiceDescriptor> Descriptors { get; }
    public Hash ContractHash { get; set; }
    public Timestamp LastUsedTime { get; set; }
    public string ContractVersion { get; set; }

    private readonly Runtime _runtime;

    public Executive(IExternalEnvironment ext, byte[] wasmCode)
    {
        _runtime = new Runtime(ext, wasmCode);
    }

    public IExecutive SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext)
    {
        return this;
    }

    public Task ApplyAsync(ITransactionContext transactionContext)
    {
        var tx = transactionContext.Transaction;
        var selector = tx.MethodName;
        var parameters = tx.Params.ToHex();
        _runtime.Input = Encoders.Hex.DecodeData(selector + parameters);
        var instance = _runtime.Instantiate();
        var call = instance.GetAction("call");
        if (call is null)
        {
            Console.WriteLine("error: call export is missing");
            return Task.CompletedTask;
        }

        try
        {
            call.Invoke();
        }
        catch (TrapException ex)
        {
            //Console.WriteLine("got exception " + ex.Message);
        }

        transactionContext.Trace.ReturnValue = ByteString.CopyFrom(_runtime.ReturnBuffer);
        transactionContext.Trace.ExecutionStatus = ExecutionStatus.Executed;
        return Task.CompletedTask;
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
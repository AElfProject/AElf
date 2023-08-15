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
    public Hash ContractHash { get; }
    public Timestamp LastUsedTime { get; set; }
    public string ContractVersion { get; }

    private readonly Runtime _runtime;

    public Executive(IExternalEnvironment ext, byte[] wasmCode)
    {
        _runtime = new Runtime(ext, wasmCode);
    }

    public IExecutive SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext)
    {
        throw new NotImplementedException();
    }

    public Task ApplyAsync(ITransactionContext transactionContext)
    {
        var tx = transactionContext.Transaction;
        var selector = tx.MethodName;
        var parameters = !tx.Params.Any()
            ? string.Empty
            : GetParameters(selector, tx.Params);
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
        return Task.CompletedTask;
    }

    /// <summary>
    /// TODO: Analysis selector & tx.Params
    /// </summary>
    /// <param name="methodName"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    private string GetParameters(string methodName, ByteString param)
    {
        // var abiEncode = new ABIEncode();
        // abiEncode.GetABIEncoded(new ABIValue("uint256", values[0])).ToHex();
        return string.Empty;
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
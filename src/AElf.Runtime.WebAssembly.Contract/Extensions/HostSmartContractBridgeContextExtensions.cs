using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly.Contract;

public static class HostSmartContractBridgeContextExtensions
{
    public static byte[] CallMethod(this IHostSmartContractBridgeContext hostSmartContractBridgeContext,
        Address fromAddress, Address toAddress, string methodName, ByteString args)
    {
        var trace = hostSmartContractBridgeContext.Execute(fromAddress, toAddress, methodName, args);

        if (!trace.IsSuccessful()) throw new ContractExecuteException(trace.Error);

        hostSmartContractBridgeContext.TransactionContext.Trace.CallStateSet = trace.StateSet;

        return trace.ReturnValue.ToByteArray();
    }
    
    public static byte[] DelegateCall(this IHostSmartContractBridgeContext hostSmartContractBridgeContext,
        Address fromAddress, Address toAddress, string methodName, ByteString args)
    {
        var trace = hostSmartContractBridgeContext.Execute(fromAddress, toAddress, methodName, args);

        if (!trace.IsSuccessful()) throw new ContractExecuteException(trace.Error);

        hostSmartContractBridgeContext.TransactionContext.Trace.DelegateCallStateSet = trace.StateSet;

        return trace.ReturnValue.ToByteArray();
    }
}
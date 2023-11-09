using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
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

        var txContext = hostSmartContractBridgeContext.TransactionContext;
        txContext.Trace.CallStateSet = trace.StateSet;
        (txContext.StateCache as TieredStateCache)?.Update(new[] { trace.StateSet });
        hostSmartContractBridgeContext.TransactionContext = txContext;

        return trace.ReturnValue.ToByteArray();
    }

    public static byte[] DelegateCall(this IHostSmartContractBridgeContext hostSmartContractBridgeContext,
        Address fromAddress, Address toAddress, string methodName, ByteString args)
    {
        var trace = hostSmartContractBridgeContext.Execute(fromAddress, toAddress, methodName, args);

        if (!trace.IsSuccessful()) throw new ContractExecuteException(trace.Error);

        var txContext = hostSmartContractBridgeContext.TransactionContext;
        txContext.Trace.CallStateSet = trace.StateSet;
        (txContext.StateCache as TieredStateCache)?.Update(new[] { trace.StateSet });
        hostSmartContractBridgeContext.TransactionContext = txContext;

        return trace.ReturnValue.ToByteArray();
    }
}
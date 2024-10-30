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
        if (txContext.Trace.CallStateSet == null)
        {
            txContext.Trace.CallStateSet = trace.StateSet;
        }
        else
        {
            foreach (var writes in trace.StateSet.Writes)
            {
                txContext.Trace.CallStateSet.Writes[writes.Key] = writes.Value;
            }
        }

        txContext.Trace.Logs.AddRange(trace.Logs);

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
        if (txContext.Trace.DelegateCallStateSet == null)
        {
            txContext.Trace.DelegateCallStateSet = trace.StateSet;
        }
        else
        {
            foreach (var writes in trace.StateSet.Writes)
            {
                txContext.Trace.DelegateCallStateSet.Writes[writes.Key] = writes.Value;
            }
        }

        (txContext.StateCache as TieredStateCache)?.Update(new[] { trace.StateSet });
        hostSmartContractBridgeContext.TransactionContext = txContext;

        return trace.ReturnValue.ToByteArray();
    }
}
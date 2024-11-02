using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Runtime.WebAssembly.TransactionPayment.Extensions;

public static class TransactionTraceExtension
{
    public static long GetEstimatedGasFee(this TransactionTrace transactionTrace)
    {
        var logEvent = transactionTrace.Logs.LastOrDefault(l =>
            l.Name == WebAssemblyTransactionPaymentConstants.GasFeeEstimatedLogEventName)?.NonIndexed;
        if (logEvent == null)
        {
            return 0;
        }

        var gasFee = new Int64Value();
        gasFee.MergeFrom(logEvent);
        return gasFee.Value;
    }

    public static long GetConsumedGasFee(this TransactionTrace transactionTrace)
    {
        var logEvent = transactionTrace.Logs.LastOrDefault(l =>
            l.Name == WebAssemblyTransactionPaymentConstants.GasFeeChargedLogEventName)?.NonIndexed;
        if (logEvent == null)
        {
            return 0;
        }

        var gasFee = new Int64Value();
        gasFee.MergeFrom(logEvent);
        return gasFee.Value;
    }
}
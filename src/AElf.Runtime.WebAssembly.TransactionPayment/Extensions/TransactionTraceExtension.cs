using AElf.Kernel;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly.TransactionPayment.Extensions;

public static class TransactionTraceExtension
{
    public static Weight? GetEstimatedGasFee(this TransactionTrace transactionTrace)
    {
        var logEvent = transactionTrace.Logs.LastOrDefault(l =>
            l.Name == WebAssemblyTransactionPaymentConstants.GasFeeEstimatedLogEventName)?.NonIndexed;
        if (logEvent == null)
        {
            return null;
        }

        var weight = new Weight();
        weight.MergeFrom(logEvent);
        weight = new Weight
        {
            RefTime = -weight.RefTime,
            ProofSize = -weight.ProofSize,
        };
        return weight;
    }

    public static Weight? GetConsumedGasFee(this TransactionTrace transactionTrace)
    {
        var logEvent = transactionTrace.Logs.LastOrDefault(l =>
            l.Name == WebAssemblyTransactionPaymentConstants.GasFeeConsumedLogEventName)?.NonIndexed;
        if (logEvent == null)
        {
            return null;
        }

        var weight = new Weight();
        weight.MergeFrom(logEvent);
        return weight;
    }
}
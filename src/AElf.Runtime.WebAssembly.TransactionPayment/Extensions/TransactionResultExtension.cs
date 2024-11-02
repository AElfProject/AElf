using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Runtime.WebAssembly.TransactionPayment.Extensions;

public static class TransactionResultExtension
{
    public static long GetEstimatedGasFee(this TransactionResult transactionResult)
    {
        var logEvent = transactionResult.Logs.LastOrDefault(l =>
            l.Name == WebAssemblyTransactionPaymentConstants.GasFeeChargedLogEventName)?.NonIndexed;
        if (logEvent == null)
        {
            return 0;
        }

        var gasFee = new Int64Value();
        gasFee.MergeFrom(logEvent);
        return gasFee.Value;
    }
    
    public static long GetChargedGasFee(this TransactionResult transactionResult)
    {
        var logEvent = transactionResult.Logs.LastOrDefault(l =>
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
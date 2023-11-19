using AElf.Types;
using Google.Protobuf;

namespace AElf.Runtime.WebAssembly.TransactionPayment.Extensions;

public static class TransactionResultExtension
{
    public static Weight? GetEstimatedGasFee(this TransactionResult transactionResult)
    {
        var logEvent = transactionResult.Logs.LastOrDefault(l =>
            l.Name == WebAssemblyTransactionPaymentConstants.GasFeeConsumedLogEventName)?.NonIndexed;
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
}
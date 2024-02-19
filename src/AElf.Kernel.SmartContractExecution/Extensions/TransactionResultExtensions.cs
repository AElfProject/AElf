using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core.Extension;
using AElf.CSharp.Core.Utils;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution.Extensions;

public static class TransactionResultExtensions
{
    public static List<Hash> GetLeafNodeList(this TransactionResult transactionResult)
    {
        var nodeList = new List<Hash>();
        nodeList.Add(GetHashCombiningTransactionAndStatus(transactionResult.TransactionId, transactionResult.Status));
        var logEventList = transactionResult.Logs.Where(log =>
            log.Name.Equals(nameof(VirtualTransactionBlocked))).ToList();
        if (transactionResult.Status != TransactionResultStatus.Mined || !logEventList.Any())
        {
            return nodeList;
        }

        for (int i = 0; i < logEventList.Count; i++)
        {
            var logEvent = logEventList[i];
            var virtualTransactionBlocked = new VirtualTransactionBlocked();
            virtualTransactionBlocked.MergeFrom(logEvent);
            var inlineTransactionId = new InlineTransaction
            {
                From = virtualTransactionBlocked.From,
                To = virtualTransactionBlocked.To,
                MethodName = virtualTransactionBlocked.MethodName,
                Params = virtualTransactionBlocked.Params,
                OriginTransactionId = transactionResult.TransactionId,
                Index = i + 1
            }.GetHash();
            nodeList.Add(GetHashCombiningTransactionAndStatus(inlineTransactionId, transactionResult.Status));
        }

        return nodeList;
    }

    private static Hash GetHashCombiningTransactionAndStatus(Hash txId,
        TransactionResultStatus executionReturnStatus)
    {
        // combine tx result status
        var rawBytes = ByteArrayHelper.ConcatArrays(txId.ToByteArray(),
            EncodingHelper.EncodeUtf8(executionReturnStatus.ToString()));
        return HashHelper.ComputeFrom(rawBytes);
    }
}
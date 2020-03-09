using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
using AElf.Types;

namespace AElf.Contracts.TestKit
{
    public static class TransactionResultExtensions
    {
        public static Dictionary<string, long> GetChargedTransactionFees(this TransactionResult transactionResult)
        {
            var relatedLog = transactionResult.Logs.FirstOrDefault(l => l.Name == nameof(TransactionFeeCharged));
            if (relatedLog == null) return new Dictionary<string, long>();
            return TransactionFeeCharged.Parser.ParseFrom(relatedLog.NonIndexed).ChargedFees
                .ToDictionary(f => f.Key, f => f.Value);
        }

        public static Dictionary<string, long> GetConsumedResourceTokens(this TransactionResult transactionResult)
        {
            var relatedLog = transactionResult.Logs.FirstOrDefault(l => l.Name == nameof(ResourceTokenCharged));
            if (relatedLog == null) return new Dictionary<string, long>();
            return ResourceTokenCharged.Parser.ParseFrom(relatedLog.NonIndexed).ChargedTokens
                .ToDictionary(f => f.Key, f => f.Value);
        }
        
        public static Dictionary<string, long> GetOwningResourceTokens(this TransactionResult transactionResult)
        {
            var relatedLog = transactionResult.Logs.FirstOrDefault(l => l.Name == nameof(ResourceTokenCharged));
            if (relatedLog == null) return new Dictionary<string, long>();
            return ResourceTokenCharged.Parser.ParseFrom(relatedLog.NonIndexed).OwingTokens
                .ToDictionary(f => f.Key, f => f.Value);
        }
    }
}
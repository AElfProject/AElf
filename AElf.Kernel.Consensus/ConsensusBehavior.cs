using System;

namespace AElf.Kernel.Consensus
{
    public enum ConsensusBehavior
    {
        NoOperationPerformed = 0,
        InitialTerm,
        NextTerm,
        SnapshotForTerm,
        SnapshotForMiners,
        SendDividends,
        NextRound,
        PackageOutValue,

        BroadcastInValue = 11
    }

    public static class ConsensusBehaviorExtensions
    {
        private static bool ShouldNotBroadcast(this ConsensusBehavior behavior)
        {
            return (int) behavior < 10;
        }

        public static bool ShouldNotBroadcast(this string str)
        {
            return Enum.TryParse(str, out ConsensusBehavior behavior) && behavior.ShouldNotBroadcast();
        }

        public static bool ShouldNotBroadcast(this Transaction tx)
        {
            return tx.Type == TransactionType.DposTransaction && tx.MethodName.ShouldNotBroadcast();
        }
    }
}
using System;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable InconsistentNaming
    public enum ConsensusBehavior
    {
        DoNothing = 0,
        InitializeAElfDPoS,
        UpdateAElfDPoS,
        PublishOutValueAndSignature,
        
        PublishInValue = 11
    }

    public static class ConsensusBehaviorExtensions
    {
        private static bool ShouldBroadcast(this ConsensusBehavior behavior)
        {
            return (int) behavior > 10;
        }
        
        public static bool ShouldBroadcast(this string str)
        {
            return Enum.TryParse(str, out ConsensusBehavior behavior) && behavior.ShouldBroadcast();
        }
        
        private static bool CanBeAddedToTxPool(this ConsensusBehavior behavior)
        {
            return (int) behavior > 10;
        }
        
        public static bool CanBeAddedToTxPool(this string str)
        {
            return Enum.TryParse(str, out ConsensusBehavior behavior) && behavior.CanBeAddedToTxPool();
        }
    }
}
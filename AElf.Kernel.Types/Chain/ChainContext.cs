using AElf.Common;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public class ChainContext : IChainContext
    {
        public int ChainId { get; set; }
        public ulong BlockHeight { get; set; }
        public Hash BlockHash { get; set; }
        public IStateCache StateCache { get; set; } 
    }
}
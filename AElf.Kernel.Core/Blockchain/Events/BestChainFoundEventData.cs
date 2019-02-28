using System.Collections.Generic;
using AElf.Common;

namespace AElf.Kernel.Blockchain.Events
{
    public class BestChainFoundEventData
    {
        public int ChainId { get; set; }
        public Hash BlockHash { get; set; }
        public ulong BlockHeight { get; set; }
        
        public List<Hash> ExecutedBlocks { get; set; }
    }
}
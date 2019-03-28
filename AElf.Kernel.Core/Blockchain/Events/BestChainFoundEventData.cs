using System.Collections.Generic;
using AElf.Common;

namespace AElf.Kernel.Blockchain.Events
{
    public class BestChainFoundEventData
    {
        public Hash BlockHash { get; set; }
        public long BlockHeight { get; set; }

        public List<Hash> ExecutedBlocks { get; set; }
    }
}
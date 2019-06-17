using System;
using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.Blockchain.Events
{
    public class BestChainFoundEvent
    {
        public Hash BlockHash { get; set; }
        public long BlockHeight { get; set; }
        
        public List<Hash> ExecutedBlocks { get; set; }
        
        public DateTime CreateTime { get; set; }
    }
}
using System;

namespace AElf.Kernel.Blockchain.Events
{
    public class BlockAcceptedEvent
    {
        public BlockHeader BlockHeader { get; set; }
        
        public bool HasFork { get; set; }
    }
}
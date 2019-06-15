using System;
using AElf.Types;

namespace AElf.Kernel.Blockchain.Events
{
    public class NewIrreversibleBlockFoundEvent
    {
        public Hash PreviousIrreversibleBlockHash { get; set; }
        public long PreviousIrreversibleBlockHeight { get; set; }
        public Hash BlockHash { get; set; }
        public long BlockHeight { get; set; }
        
        public DateTime CreateTime { get; set; }
    }
}
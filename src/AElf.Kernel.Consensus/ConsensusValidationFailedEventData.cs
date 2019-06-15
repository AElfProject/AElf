using System;

namespace AElf.Kernel.Consensus
{
    public class ConsensusValidationFailedEventData
    {
        public string ValidationResultMessage { get; set; }
        
        public DateTime CreateTime { get; set; }
    }
}
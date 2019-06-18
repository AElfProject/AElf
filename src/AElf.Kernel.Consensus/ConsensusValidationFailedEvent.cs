using System;

namespace AElf.Kernel.Consensus
{
    public class ConsensusValidationFailedEvent
    {
        public string ValidationResultMessage { get; set; }
    }
}
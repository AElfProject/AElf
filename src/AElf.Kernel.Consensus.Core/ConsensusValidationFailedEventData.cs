namespace AElf.Kernel.Consensus
{
    public class ConsensusValidationFailedEventData
    {
        public string ValidationResultMessage { get; set; }
        public bool IsReTrigger { get; set; }
    }
}
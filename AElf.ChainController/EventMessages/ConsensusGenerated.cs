namespace AElf.ChainController.EventMessages
{
    public class ConsensusGenerated
    {
        public bool IsGenerated { get; }

        public ConsensusGenerated(bool isGenerated)
        {
            IsGenerated = isGenerated;
        }
    }
}
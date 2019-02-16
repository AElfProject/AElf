namespace AElf.Kernel
{
    public class BlockMiningEventData
    {
        public int ChainId { get; set; }

        public BlockMiningEventData(int chainId)
        {
            ChainId = chainId;
        }
    }
}
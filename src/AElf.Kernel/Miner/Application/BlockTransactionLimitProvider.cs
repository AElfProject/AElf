namespace AElf.Kernel.Miner.Application
{
    public class BlockTransactionLimitProvider : IBlockTransactionLimitProvider
    {
        public int Limit { get; set; } = 0;
    }
}
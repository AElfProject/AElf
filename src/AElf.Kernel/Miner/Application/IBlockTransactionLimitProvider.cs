namespace AElf.Kernel.Miner.Application
{
    public interface IBlockTransactionLimitProvider
    {
        int Limit { get; set; }
    }
}
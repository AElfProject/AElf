namespace AElf.Kernel.MIner
{
    public interface IMinerConfig
    {
        Hash CoinBase { get; set; }
        bool IsParallel { get; set; }
        Hash ChainId { get; set; }
        
    }
}
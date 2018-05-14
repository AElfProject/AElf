namespace AElf.Kernel.MIner
{
    public class MinerConifg : IMinerConfig
    {
        public Hash CoinBase { get; set; }
        public bool IsParallel { get; set; }
        public Hash ChainId { get; set; }
    }
}
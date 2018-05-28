namespace AElf.Kernel.Miner
{
    public class MinerConifg : IMinerConfig
    {
        public Hash CoinBase { get; set; }
        public bool IsParallel { get; set; }
        public Hash ChainId { get; set; }
        public ulong TxCountLimit { get; set; }
        
        public static MinerConifg Default = new MinerConifg
        {
            CoinBase = Hash.Generate(),
            IsParallel = true,
            ChainId = Hash.Generate(),
            TxCountLimit = 10
        };
    }
}
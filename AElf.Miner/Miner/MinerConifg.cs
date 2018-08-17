using AElf.ChainController;
using AElf.Kernel;

namespace AElf.Miner.Miner
{
    public class MinerConfig : IMinerConfig
    {
        public Hash CoinBase { get; set; }
        public bool IsParallel { get; } = true;
        public Hash ChainId { get; set; }

        public static MinerConfig Default = new MinerConfig
        {
            CoinBase = Hash.Generate()
        };
    }
}
﻿using AElf.Kernel.Types;

namespace AElf.Kernel.Miner
{
    public class MinerConfig : IMinerConfig
    {
        public Hash CoinBase { get; set; }
        public bool IsParallel { get; } = true;
        public Hash ChainId { get; set; }
        public ulong TxCount { get; set; }
        
        public static MinerConfig Default = new MinerConfig
        {
            CoinBase = Hash.Generate(),
            TxCount = 10
        };
    }
}
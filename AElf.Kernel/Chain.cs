using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class Chain : IChain
    {
        public long CurrentBlockHeight { get; set; }
        public IHash<IBlock> CurrentBlockHash { get; set; }
        public void UpdateCurrentBlock(IBlock block)
        {
            CurrentBlockHeight += 1;
            CurrentBlockHash = block.GetHash();
        }

        public IHash<IChain> Id { get; set; }
        public IHash<IBlock> GenesisBlockHash { get; set; }
    }
}
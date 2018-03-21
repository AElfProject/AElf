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
        public Hash CurrentBlockHash { get; set; }
        public void UpdateCurrentBlock(IBlock block)
        {
            CurrentBlockHeight += 1;
            CurrentBlockHash = block.GetHash();
        }

        public Hash Id { get; set; }
        public Hash GenesisBlockHash { get; set; }

        public Chain():this(Hash.Zero)
        {
            
        }

        public Chain(Hash id)
        {
            Id = id;
        }
    }
}
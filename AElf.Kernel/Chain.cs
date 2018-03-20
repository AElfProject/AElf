using System;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class Chain : IChain
    {
        
        public Chain(IHash<IBlock> genesisBlockHash)
        {
            GenesisBlockHash = genesisBlockHash;
            Id = new Hash<IChain>(genesisBlockHash.Value);
            CurrentBlockHeight = 0;
        }
        
        public long CurrentBlockHeight { get; private set; }
        public IHash<IBlock> CurrentBlockHash { get; private set; }
        public void UpdateCurrentBlock(IBlock block)
        {
            CurrentBlockHeight += 1;
            CurrentBlockHash = block.GetHash();
        }

        public IHash<IChain> Id { get; }
        public IHash<IBlock> GenesisBlockHash { get; }
    }
}
// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class Chain
    {
        public void UpdateCurrentBlock(Block block)
        {
            block.Header.Index = CurrentBlockHeight;
            CurrentBlockHeight += 1;
            CurrentBlockHash = block.GetHash();
        }
        public Hash GenesisBlockHash { get; set; }

        public Chain(Hash id)
        {
            Id = id;
            CurrentBlockHash = null;
            CurrentBlockHeight = 0;
        }
    }
}
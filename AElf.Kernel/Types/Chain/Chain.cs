namespace AElf.Kernel
{
    public class Chain
    {
        public ulong CurrentBlockHeight { get; set; }
        public Hash CurrentBlockHash { get; set; }
        
        public void UpdateCurrentBlock(Block block)
        {
            block.Header.Index = CurrentBlockHeight;
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
            CurrentBlockHash = null;
            CurrentBlockHeight = 0;
        }
    }
}
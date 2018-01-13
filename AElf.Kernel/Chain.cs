using System.Collections.Generic;

namespace AElf.Kernel
{
    public class Chain : IChain
    {
        public List<Block> Blocks { get; set; } = new List<Block>();

        public long CurrentBlockHeight
        {
            get
            {
                return Blocks.Count;
            }
        }

        public IHash<IBlock> CurrentBlockHash
        {
            get
            {
                return new Hash<IBlock>(Blocks[Blocks.Count - 1].BlockHeader.MerkleRootHash.Value);
            }
        }

    }
}
using System.Collections.Generic;

namespace AElf.Kernel
{
    public class Chain : IChain
    {
        private List<IBlock> Blocks
        {
            get
            {
                return new List<IBlock>();

                //TODO:
                //Maybe get the blocks from database.
            }
        }

        public long CurrentBlockHeight => Blocks.Count;

        public IHash<IBlock> CurrentBlockHash => new Hash<IBlock>(Blocks[Blocks.Count - 1].GetHash().Value);
    }
}
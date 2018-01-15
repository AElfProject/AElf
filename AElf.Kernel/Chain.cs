using System.Collections.Generic;

namespace AElf.Kernel
{
    public class Chain : IChain
    {
        /// <summary>
        /// A memory based block storage
        /// </summary>
        /// <value>The blocks.</value>
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
                return new Hash<IBlock>(Blocks[Blocks.Count - 1].GetHeader().GetTransactionMerkleTreeRoot().Value);
            }
        }

    }
}
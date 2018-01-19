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
            }
        }

        public long CurrentBlockHeight => Blocks.Count;

        public IHash<IBlock> CurrentBlockHash => new Hash<IBlock>(Blocks[Blocks.Count - 1].GetHash().Value);

        public IHash<IAccount> CurrentBlockStateHash => new Hash<IAccount>(
            Blocks[Blocks.Count - 1].GetHeader().GetStateMerkleTreeRoot().Value);

        public void AddBlock(IBlock block)
        {
            Blocks.Add(block);
        }
    }
   
}
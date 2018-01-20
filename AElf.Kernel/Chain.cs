using System.Collections.Generic;

namespace AElf.Kernel
{
    public class Chain : IChain
    {
        private List<IBlock> _blocks = new List<IBlock>();

        public long CurrentBlockHeight => _blocks.Count;

        public IHash<IBlock> CurrentBlockHash => new Hash<IBlock>(_blocks[_blocks.Count - 1].GetHash().Value);

        public IHash<IAccount> CurrentBlockStateHash => new Hash<IAccount>(
            _blocks[_blocks.Count - 1].GetHeader().GetStateMerkleTreeRoot().Value);

        public void AddBlock(IBlock block)
        {
            _blocks.Add(block);
        }
    }
   
}
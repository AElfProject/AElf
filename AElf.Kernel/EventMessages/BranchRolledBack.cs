using System.Collections.Generic;

namespace AElf.Kernel.EventMessages
{
    public sealed class BranchRolledBack
    {
        public BranchRolledBack(List<Block> blocks)
        {
            Blocks = blocks;
        }

        public List<Block> Blocks { get; }
    }
}
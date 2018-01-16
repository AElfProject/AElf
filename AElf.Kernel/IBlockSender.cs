using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Kernel
{
    public interface IBlockSender
    {
        void BroadcastBlock(IBlock block);
    }
}

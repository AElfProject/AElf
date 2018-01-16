using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Kernel
{
    /// <summary>
    /// Use the received transactions produce a block
    /// </summary>
    public interface IBlockProducer
    {
        IBlock CreateBlock();
    }
}
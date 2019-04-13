using System;
using System.Collections.Generic;

namespace AElf.Kernel
{
    public interface IBlock : IHashProvider
    {
        BlockHeader Header { get; set; }
        BlockBody Body { get; set; }
        long Height { get; set; }
        byte[] GetHashBytes();
        Block Clone();
    }
}
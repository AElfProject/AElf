using System;
using System.Collections.Generic;
using AElf.Common;

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

    public interface IBlockIndex
    {
        Hash Hash { get; }
        long Height { get; }
    }
}
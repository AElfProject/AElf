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
        string BlockHashToHex { get; set; }
        byte[] GetHashBytes();
        Block Clone();
    }
}
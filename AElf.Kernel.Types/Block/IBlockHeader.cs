using System;
using AElf.Kernel.Types;
using Google.Protobuf.Collections;
using AElf.Common;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public interface IBlockHeader : IHashProvider
    {
        int Version { get; set; }
        Hash MerkleTreeRootOfTransactions { get; set; }
        int ChainId { get; set; }
        long Height { get; set; }
    }
}
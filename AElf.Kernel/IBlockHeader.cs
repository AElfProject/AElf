using System;
using AElf.Kernel.Merkle;

namespace AElf.Kernel
{
    public interface IBlockHeader
    {

        Hash PreviousHash { get; set; }
        Int32 Version { get; set; }
        Hash MerkleTreeRootOfTransactions { get; set; }
    }
}
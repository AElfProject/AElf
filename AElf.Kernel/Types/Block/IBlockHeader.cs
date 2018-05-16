using System;

namespace AElf.Kernel
{
    public interface IBlockHeader :  IHashProvider, ISerializable
    {
        int Version { get; set; }
        Hash PreviousHash { get; }
        Hash MerkleTreeRootOfTransactions { get; set; }
    }
}
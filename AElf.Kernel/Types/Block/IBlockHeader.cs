using System;

namespace AElf.Kernel
{
    public interface IBlockHeader :  IHashProvider, ISerializable
    {
        int Version { get; set; }
        Hash PreviousHash { get; set; }
        Hash MerkleTreeRootOfTransactions { get; set; }
    }
}
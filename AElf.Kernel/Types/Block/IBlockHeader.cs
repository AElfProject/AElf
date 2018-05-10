using System;

namespace AElf.Kernel
{
    public interface IBlockHeader :  IHashProvider
    {
        Int32 Version { get; set; }
        
        Hash PreviousHash { get; set; }
        Hash MerkleTreeRootOfTransactions { get; set; }
    }
}
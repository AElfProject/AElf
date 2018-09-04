using System;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Types;

namespace AElf.Kernel
{
    public interface IBlockHeader : IHashProvider
    {
        int Version { get; set; }
        Hash MerkleTreeRootOfTransactions { get; set; }
        ECSignature GetSignature();
        Hash ChainId { get; set; }
    }
}
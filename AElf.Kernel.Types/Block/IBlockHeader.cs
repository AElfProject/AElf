using System;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Types;

namespace AElf.Kernel
{
    public interface IBlockHeader : IHashProvider, ISerializable
    {
        int Version { get; set; }
        Hash MerkleTreeRootOfTransactions { get; set; }
        ECSignature GetSignature();
    }
}
using System;
using AElf.Cryptography.ECDSA;
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
        ECSignature GetSignature();
        Hash ChainId { get; set; }
        Hash SideChainTransactionsRoot { get; set; }
        Hash SideChainBlockHeadersRoot { get; set; }
        ulong Index { get; set; }
    }
}
using AElf.Types;

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
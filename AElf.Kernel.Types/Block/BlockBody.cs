using System.Collections.Generic;
using System.Linq;
using AElf.Common;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class BlockBody : IBlockBody
    {
        private Hash _blockBodyHash;
        public int TransactionsCount => Transactions.Count;
        public BinaryMerkleTree BinaryMerkleTree { get; } = new BinaryMerkleTree();

        //TODO: Add GetHash test case [Case]
        /// <inheritdoc />
        public Hash GetHash()
        {
            return _blockBodyHash ?? CalculateBodyHash();
        }

        public Hash GetHashWithoutCache()
        {
            _blockBodyHash = null;
            return GetHash();
        }

        private Hash CalculateBodyHash()
        {
            _blockBodyHash = new List<Hash>
            {
                BlockHeader,
                BinaryMerkleTree.Root
            }.Aggregate(Hash.FromTwoHashes);
            return _blockBodyHash;
        }
    }
}
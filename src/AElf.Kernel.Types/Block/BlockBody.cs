using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel
{
    public partial class BlockBody : IBlockBody
    {
        public int TransactionsCount => Transactions.Count;
        private Hash _blockBodyHash;
        public BinaryMerkleTree BinaryMerkleTree { get; } = new BinaryMerkleTree();

        private Hash CalculateBodyHash()
        {
            _blockBodyHash = new List<Hash>()
            {
                BlockHeader,
                BinaryMerkleTree.Root
            }.Aggregate(Hash.FromTwoHashes);
            return _blockBodyHash;
        }

        /// <inheritdoc/>
        public Hash GetHash()
        {
            return _blockBodyHash ?? CalculateBodyHash();
        }

        public Hash GetHashWithoutCache()
        {
            _blockBodyHash = null;
            return GetHash();
        }
    }
}
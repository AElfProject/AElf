using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel
{
    public partial class BlockBody : IBlockBody
    {
        public int TransactionsCount => Transactions.Count;
        private Hash _blockBodyHash;
        // TODO: remove
        public BinaryMerkleTree BinaryMerkleTree { get; } = new BinaryMerkleTree();


        // TODO: remove
        private Hash CalculateBodyHash()
        {
            _blockBodyHash = new List<Hash>()
            {
                BlockHeader,
                BinaryMerkleTree.Root
            }.Aggregate(Hash.FromTwoHashes);
            return _blockBodyHash;
        }

        // TODO: check to remove
        /// <inheritdoc/>
        public Hash GetHash()
        {
            return _blockBodyHash ?? CalculateBodyHash();
        }

        // TODO: check to remove
        public Hash GetHashWithoutCache()
        {
            _blockBodyHash = null;
            return GetHash();
        }
    }
}
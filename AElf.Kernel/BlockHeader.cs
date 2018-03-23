using AElf.Kernel.Extensions;
using AElf.Kernel.Merkle;
using System;

namespace AElf.Kernel
{
    [Serializable]
    public class BlockHeader : IBlockHeader
    {
        /// <summary>
        /// Blockchain version.
        /// </summary>
        public const uint Version = 0x1;


        /// <summary>
        /// The miner's signature.
        /// </summary>
        public byte[] Signatures;

        private BinaryMerkleTree _transactionMerkleTree = new BinaryMerkleTree();

        /// <summary>
        /// the timestamp of this block
        /// </summary>
        /// <value>The time stamp.</value>
        public long TimeStamp => (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;

        public BlockHeader(Hash preBlockHash)
        {
            PreviousHash = preBlockHash;
        }

        /// <summary>
        /// include transactions into the merkle tree
        /// </summary>
        /// <param name="hash">Hash.</param>
        public void AddTransaction(Hash hash)
        {
            _transactionMerkleTree.AddNode(hash);
        }

        public Hash PreviousHash { get; set; }
        public Hash Hash { get; set; }

        /// <summary>
        /// Gets the transaction merkle tree root.
        /// </summary>
        /// <returns>The transaction merkle tree root.</returns>
        public Hash GetTransactionMerkleTreeRoot()
        {
            return _transactionMerkleTree.ComputeRootHash();
        }

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    }
}
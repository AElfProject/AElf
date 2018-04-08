using AElf.Kernel.Extensions;
using AElf.Kernel.Merkle;
using System;

namespace AElf.Kernel
{
    public partial class BlockHeader : IBlockHeader, IHashProvider
    {
        /// <summary>
        /// The miner's signature.
        /// </summary>
        public byte[] Signatures;

        /// <summary>
        /// The merkle root hash of all the transactions in this block
        /// </summary>
        public IHash MerkleRootHash => GetTransactionMerkleTreeRoot();

        private readonly BinaryMerkleTree _transactionMerkleTree = new BinaryMerkleTree();

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
        

        /// <summary>
        /// Gets the transaction merkle tree root.
        /// </summary>
        /// <returns>The transaction merkle tree root.</returns>
        public Hash GetTransactionMerkleTreeRoot()
        {
            return _transactionMerkleTree.ComputeRootHash();
        }

        public Hash GetHash()
        {
            return new Hash( this.CalculateHash() );
        }
    }
}
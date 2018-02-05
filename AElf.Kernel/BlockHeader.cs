using AElf.Kernel.Extensions;
using AElf.Kernel.Merkle;
using System;

namespace AElf.Kernel
{
    [Serializable]
    public class BlockHeader : IBlockHeader
    {
        /// <summary>
        /// AELF version magic words
        /// </summary>
        /// <value>The version.</value>
        public const int Version = 0x1;

        /// <summary>
        /// points to previous block hash 
        /// </summary>
        /// <value>The pre block hash.</value>
        private IHash<IBlock> _preBlockHash;

        /// <summary>
        /// The miner's signature.
        /// </summary>
        public byte[] Signatures;

        /// <summary>
        /// the merkle root hash
        /// </summary>
        /// <value>The merkle root hash.</value>
        public IHash<IMerkleTree<ITransaction>> MerkleRootHash => GetTransactionMerkleTreeRoot();

        private readonly BinaryMerkleTree<ITransaction> _transactionMerkleTree = new BinaryMerkleTree<ITransaction>();

        /// <summary>
        /// the timestamp of this block
        /// </summary>
        /// <value>The time stamp.</value>
        public long TimeStamp => (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;

        public BlockHeader(IHash<IBlock> preBlockHash)
        {
            _preBlockHash = preBlockHash;
        }

        /// <summary>
        /// include transactions into the merkle tree
        /// </summary>
        /// <param name="hash">Hash.</param>
        public void AddTransaction(IHash<ITransaction> hash)
        {
            _transactionMerkleTree.AddNode(hash);
        }

        /// <summary>
        /// Gets the transaction merkle tree root.
        /// </summary>
        /// <returns>The transaction merkle tree root.</returns>
        public IHash<IMerkleTree<ITransaction>> GetTransactionMerkleTreeRoot()
        {
            return _transactionMerkleTree.ComputeRootHash();
        }

        /// <summary>
        /// Gets the block hash.
        /// </summary>
        /// <returns>The hash.</returns>
        public IHash<IBlockHeader> GetHash()
        {
            return new Hash<IBlockHeader>(this.CalculateHash());
        }
    }
}
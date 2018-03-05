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
        /// The pre block's hash value.
        /// </summary>
        private IHash<IBlock> _preBlockHash;

        /// <summary>
        /// The miner's signature.
        /// </summary>
        public byte[] Signatures;

        /// <summary>
        /// The merkle root hash of all the transactions in this block
        /// </summary>
        public IHash<IMerkleTree<ITransaction>> MerkleRootHash => GetTransactionMerkleTreeRoot();

        private BinaryMerkleTree<ITransaction> _transactionMerkleTree = new BinaryMerkleTree<ITransaction>();

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

        public byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    }
}
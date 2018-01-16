using System;

namespace AElf.Kernel
{
    [Serializable]
    public class BlockHeader : IBlockHeader
    {
        /// <summary>
        /// The hash value of pervious block.
        /// Called 'parentHash' in Ethereum.
        /// </summary>
        public IHash<IBlock> PreBlockHash { get; protected set; }

        /// <summary>
        /// Time stamp.
        /// </summary>
        public long TimeStamp => (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;

        /// <summary>
        /// Should use the hash value of previous block to generate
        /// a new block.
        /// </summary>
        /// <param name="preBlockHash"></param>
        public BlockHeader(IHash<IBlock> preBlockHash)
        {
            PreBlockHash = preBlockHash;
        }

        /// <summary>
        /// The difficulty of mining.
        /// </summary>
        public int Bits => GetBits();

        /// <summary>
        /// Random value.
        /// </summary>
        public int Nonce { get; set; }

        #region Private fields
        private MerkleTree<ITransaction> _transactionTrie = new MerkleTree<ITransaction>();
        #endregion

        /// <summary>
        /// Just add the hash value of each transaction.
        /// For the block header should be very small.
        /// </summary>
        /// <param name="hash"></param>
        public void AddTransaction(IHash<ITransaction> hash)
        {
            _transactionTrie.AddNode(hash);
        }

        /// <summary>
        /// Get the merkle root hash value of transactions.
        /// </summary>
        public IHash<IMerkleTree<ITransaction>> GetTransactionMerkleTreeRoot()
        {
            return _transactionTrie.ComputeRootHash();
        }

        /// <summary>
        /// Get the difficulty of mining.
        /// </summary>
        /// <returns></returns>
        private int GetBits()
        {
            return 1;
        }

        public IHash<IBlockHeader> GetHash()
        {
            return new Hash<IBlockHeader>(ExtensionMethods.GetHash(this));
        }
    }
}
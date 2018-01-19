using System;
using System.Collections.Generic;

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

        public IHash<IMerkleTree<IAccount>> StateRootHash
        {
            get
            {
                MerkleTree<IAccount> merkle = new MerkleTree<IAccount>();
                if (_stateTireRightChild.Nodes.Count < 1)
                {
                    merkle.AddNode(_preStateRootHash);
                }
                else
                {
                    merkle.AddNodes(new List<IHash<IAccount>>{ _preStateRootHash,
                        new Hash<IAccount>(_stateTireRightChild?.ComputeRootHash().Value) });
                }

                return merkle.ComputeRootHash();
            }
        }

        private IHash<IAccount> _preStateRootHash;

        /// <summary>
        /// Time stamp.
        /// </summary>
        public long TimeStamp => (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;

        /// <summary>
        /// Should use the hash value of previous block to generate
        /// a new block.
        /// </summary>
        /// <param name="preBlockHash"></param>
        public BlockHeader(IHash<IBlock> preBlockHash, IHash<IAccount> preStateRootHash)
        {
            PreBlockHash = preBlockHash;
            _preStateRootHash = preStateRootHash;
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
        private MerkleTree<IAccount> _stateTireRightChild = new MerkleTree<IAccount>();
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

        public void AddState(IAccount account)
        {
            var hash = new Hash<IAccount>(ExtensionMethods.GetHash(account));
            _stateTireRightChild.AddNode(hash);
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

        public IHash<IMerkleTree<IAccount>> GetStateMerkleTreeRoot()
        {
            return StateRootHash;
        }
    }
}
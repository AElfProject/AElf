using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;

// ReSharper disable once CheckNamespace
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
        
        public bool AddTransaction(Transaction tx)
        {
            Transactions.Add(tx.GetHash());
            TransactionList.Add(tx);
            return true;
        }

        public bool AddTransactions(IEnumerable<Hash> txs)
        {
            Transactions.Add(txs);
            return true;
        }
        
        /// <summary>
        /// Calculate merkle tree root of transaction and side chain block info. 
        /// </summary>
        /// <returns></returns>
        public Hash CalculateMerkleTreeRoots()
        {
            // side chain info
            if (TransactionsCount == 0)
                return Hash.Default;
            if (BinaryMerkleTree.Root != null)
                return BinaryMerkleTree.Root;
            BinaryMerkleTree.AddNodes(Transactions);
            BinaryMerkleTree.ComputeRootHash();
            
            return BinaryMerkleTree.Root;
        }

        /// <inheritdoc/>
        public Hash GetHash()
        {
            return _blockBodyHash ?? CalculateBodyHash();
        }

        /// <summary>
        /// Set block header hash
        /// </summary>
        /// <param name="blockHeaderHash"></param>
        /// <param name="indexedSideChainBlockInfo"></param>
        public void Complete(Hash blockHeaderHash, SideChainBlockInfo[] indexedSideChainBlockInfo = null)
        {
            BlockHeader = blockHeaderHash;
            if (indexedSideChainBlockInfo == null)
                return;
            IndexedInfo.AddRange(indexedSideChainBlockInfo);
        }
    }
}
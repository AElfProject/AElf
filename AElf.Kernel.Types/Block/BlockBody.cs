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
        public Hash SideChainBlockHeadersRoot { get; private set; }
        public Hash SideChainTransactionsRoot{ get; private set; }
        private Hash _blockBodyHash;
        public BinaryMerkleTree BinaryMerkleTree { get; } = new BinaryMerkleTree();
        public BinaryMerkleTree BinaryMerkleTreeForSideChainTransactionRoots { get; }= new BinaryMerkleTree();

        private Hash CalculateBodyHash()
        {
            _blockBodyHash = new List<Hash>()
            {
                BlockHeader,
                BinaryMerkleTree.Root,
                SideChainBlockHeadersRoot ?? CalculateSideChainBlockHeadersRoot(),
                SideChainTransactionsRoot ?? CalculateSideChainTransactionsRoot()
            }.Aggregate(Hash.FromTwoHashes);
            return _blockBodyHash;
        }
        
        private Hash CalculateSideChainBlockHeadersRoot()
        {
            if (SideChainBlockHeadersRoot == null)
                SideChainBlockHeadersRoot = CalculateSideChainRoot(isHeaderRoot: true);
            return SideChainBlockHeadersRoot;
        }
        
        private Hash CalculateSideChainTransactionsRoot()
        {
            if (SideChainTransactionsRoot == null)
                SideChainTransactionsRoot = CalculateSideChainRoot(isHeaderRoot: false);
            return SideChainTransactionsRoot;
        }

        /// <summary>
        /// Calculate header root or transaction root
        /// </summary>
        /// <param name="isHeaderRoot"></param>
        /// <returns></returns>
        private Hash CalculateSideChainRoot(bool isHeaderRoot)
        {
            Hash res;
            if (IndexedInfo.Count != 0)
            {
                var roots = IndexedInfo
                    .Select(info => isHeaderRoot ? info.BlockHeaderHash : info.TransactionMKRoot).ToList();
                res = (isHeaderRoot ? new BinaryMerkleTree() : BinaryMerkleTreeForSideChainTransactionRoots)
                    .AddNodes(roots).ComputeRootHash();
            }
            else 
                res = Hash.Default;
            return res;
        }
        
        public bool AddTransaction(Transaction tx)
        {
            Transactions.Add(tx.GetHash());
            TransactionList.Add(tx);
            return true;
        }

        public bool AddTransactions(IEnumerable<Transaction> txs)
        {
            var collection = txs.ToList();
            Transactions.Add(collection.Select(tx => tx.GetHash()).Distinct());
            TransactionList.Add(collection);
            return true;
        }
        
        /// <summary>
        /// Calculate merkle tree root of transaction and side chain block info. 
        /// </summary>
        /// <returns></returns>
        public Hash CalculateMerkleTreeRoots()
        {
            // side chain info
            CalculateSideChainBlockHeadersRoot();
            CalculateSideChainTransactionsRoot();
            
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
        public void Complete(Hash blockHeaderHash)
        {
            BlockHeader = blockHeaderHash;
        }
    }
}
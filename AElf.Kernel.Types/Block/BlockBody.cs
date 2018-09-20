using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class BlockBody : IBlockBody
    {
        public int TransactionsCount => Transactions.Count;
        private Hash _txMtRoot;
        public Hash SideChainBlockHeadersRoot { get; private set; }
        public Hash SideChainTransactionsRoot{ get; private set; }
        private Hash _blockBodyHash;

        private Hash CalculateBodyHash()
        {
            _blockBodyHash = BlockHeader.CalculateHashWith(
                HashExtensions.CalculateHashOfHashList(_txMtRoot, SideChainBlockHeadersRoot ?? CalculateSideChainBlockHeadersRoot(),
                    SideChainTransactionsRoot?? CalculateSideChainTransactionsRoot()));
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
        /// calculate header root or transaction root
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
                res = new BinaryMerkleTree().AddNodes(roots).ComputeRootHash();
            }
            else 
                res = Hash.Default;
            return res;
        }
        
        public bool AddTransaction(Hash tx)
        {
            Transactions.Add(tx);
            return true;
        }

        public bool AddTransactions(IEnumerable<Hash> hashes)
        {
            var collection = hashes.ToList();
            Transactions.Add(collection.Distinct());
            return true;
        }
        
        /// <summary>
        /// calculate 
        /// </summary>
        /// <returns></returns>
        public Hash CalculateTransactionMerkleTreeRoot()
        {
            if (TransactionsCount == 0)
                return Hash.Default;
            if (_txMtRoot != null)
                return _txMtRoot;
            var merkleTree = new BinaryMerkleTree();
            merkleTree.AddNodes(Transactions);
            
            _txMtRoot = merkleTree.ComputeRootHash();
            CalculateSideChainBlockHeadersRoot();
            CalculateSideChainTransactionsRoot();
            return _txMtRoot;
        }

        /// <inheritdoc/>
        public Hash GetHash()
        {
            return _blockBodyHash ?? CalculateBodyHash();
        }
        
        /// <summary>
        /// set block header hash
        /// </summary>
        /// <param name="blockHeaderHash"></param>
        public void Complete(Hash blockHeaderHash)
        {
            BlockHeader = blockHeaderHash;
        }
    }
}
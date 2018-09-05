using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.Types;
using AElf.Kernel.Types.Merkle;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class BlockBody : IBlockBody
    {
        public int TransactionsCount => Transactions.Count;
        private Hash _txMtRoot;
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

        
        public Hash CalculateMerkleTreeRoot()
        {
            if (TransactionsCount == 0)
                return Hash.Default;
            if (_txMtRoot != null)
                return _txMtRoot;
            var merkleTree = new BinaryMerkleTree();
            merkleTree.AddNodes(Transactions);
            
            _txMtRoot = merkleTree.ComputeRootHash();
            return _txMtRoot;
        }

        public IBlockBody Deserialize(byte[] bytes)
        {
            return Parser.ParseFrom(bytes);
        }

        public Hash GetHash()
        {
            return BlockHeader.CalculateHashWith(_txMtRoot??CalculateMerkleTreeRoot());
        }
        
        public void Complete(Hash blockHeaderHash)
        {
            BlockHeader = blockHeaderHash;
        }
    }
}
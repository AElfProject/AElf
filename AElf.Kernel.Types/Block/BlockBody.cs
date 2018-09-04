using System.Collections.Generic;
using AElf.Kernel.Types;
using AElf.Kernel.Types.Merkle;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class BlockBody : IBlockBody
    {
        public int TransactionsCount => txs.Count;
        
        private HashSet<Hash> txs = new HashSet<Hash>();
        public bool AddTransaction(Hash tx)
        {
            return txs.Add(tx);
        }

        public Hash CalculateMerkleTreeRoot()
        {
            if (TransactionsCount == 0)
                return Hash.Default; 
            var merkleTree = new BinaryMerkleTree();
            merkleTree.AddNodes(txs);
            
            return merkleTree.ComputeRootHash();
        }

        public IBlockBody Deserialize(byte[] bytes)
        {
            return Parser.ParseFrom(bytes);
        }

        public Hash GetHash()
        {
            return BlockHeader.CalculateHashWith(CalculateMerkleTreeRoot());
        }
        
        public void Complete(Hash blockHeaderHash)
        {
            BlockHeader = blockHeaderHash;
            Transactions.Add(txs);
        }
    }
}
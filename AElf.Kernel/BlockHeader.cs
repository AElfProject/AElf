using System;

namespace AElf.Kernel
{
    public class BlockHeader : IBlockHeader
    {
        public int Version => 0;
        public IHash<IBlock> PreBlockHash { get; protected set; }
        public IHash<IMerkleTree<ITransaction>> MerkleRootHash
        {
            get
            {
                return GetTransactionMerkleTreeRoot();
            }
        }

        private MerkleTree<ITransaction> MerkleTree { get; set; }

        public long TimeStamp { get; protected set; }

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
        public int Nonce => GetNonce();

        public void AddTransaction(IHash<ITransaction> hash)
        {
            MerkleTree.AddNode(hash);
        }

        public IHash<IMerkleTree<ITransaction>> GetTransactionMerkleTreeRoot()
        {
            return MerkleTree.ComputeRootHash();
        }

        private int GetBits()
        {
            return 1;
        }

        private int GetNonce()
        {
            return new Random().Next(1, 100000);
        }
    }
}
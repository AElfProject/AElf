using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

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

        public long TimeStamp => DateTime.UtcNow.Second;

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
        public int Nonce { get; set; } = 0;

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

        public IHash<IBlockHeader> GetHash()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Nonce++;
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, this);
                return new Hash<IBlockHeader>(SHA256.Create().ComputeHash(ms));
            }
        }
    }
}
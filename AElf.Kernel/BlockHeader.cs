using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Kernel
{
    [Serializable]
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

        private MerkleTree<ITransaction> MerkleTree { get; set; } = new MerkleTree<ITransaction>();

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
        public int Nonce { get; set; }

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
            return new Hash<IBlockHeader>(this.GetSHA256Hash());
        }

        public void SetNonce()
        {
            Nonce++;
        }
    }
}
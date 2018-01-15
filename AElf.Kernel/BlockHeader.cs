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
        /// <summary>
        /// AELF version magic words
        /// </summary>
        /// <value>The version.</value>
        public const int Version = 0x1;

        /// <summary>
        /// points to previous block hash 
        /// </summary>
        /// <value>The pre block hash.</value>
        public IHash<IBlock> PreBlockHash { get; protected set; }

        /// <summary>
        /// The miner's signature.
        /// </summary>
        public byte[] Signatures;

        /// <summary>
        /// the merkle root hash
        /// </summary>
        /// <value>The merkle root hash.</value>
        public IHash<IMerkleTree<ITransaction>> MerkleRootHash
        {
            get
            {
                return GetTransactionMerkleTreeRoot();
            }
        }

        private MerkleTree<ITransaction> MerkleTree { get; set; } = new MerkleTree<ITransaction>();

        /// <summary>
        /// the timestamp of this block
        /// </summary>
        /// <value>The time stamp.</value>
        public long TimeStamp => DateTime.UtcNow.Second;

        public BlockHeader(IHash<IBlock> preBlockHash)
        {
            PreBlockHash = preBlockHash;
        }

        /// <summary>
        /// include transactions into the merkle tree
        /// </summary>
        /// <param name="hash">Hash.</param>
        public void AddTransaction(IHash<ITransaction> hash)
        {
            MerkleTree.AddNode(hash);
        }

        /// <summary>
        /// Gets the transaction merkle tree root.
        /// </summary>
        /// <returns>The transaction merkle tree root.</returns>
        public IHash<IMerkleTree<ITransaction>> GetTransactionMerkleTreeRoot()
        {
            return MerkleTree.ComputeRootHash();
        }

        /// <summary>
        /// Gets the block hash.
        /// </summary>
        /// <returns>The hash.</returns>
        public IHash<IBlockHeader> GetHash()
        {
            return new Hash<IBlockHeader>(this.GetSHA256Hash());
        }

        /// <summary>
        /// Serialize the header.
        /// </summary>
        /// <returns>serizlied header</returns>
        public byte[] Serialize()
        {
            // TODO: build binary result
            throw new NotImplementedException();
        }
    }
}
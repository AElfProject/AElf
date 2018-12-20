using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Cryptography.ECDSA;
using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel
{
    public partial class Block : IBlock
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:AElf.Kernel.Block"/> class.
        /// A previous block must be referred, except for the genesis block.
        /// </summary>
        /// <param name="preBlockHash">Pre block hash.</param>
        public Block(Hash preBlockHash)
        {
            Header = new BlockHeader(preBlockHash);
            Body = new BlockBody();
        }

        /// <summary>
        /// Add transaction Hash to the block
        /// </summary>
        /// <returns><c>true</c>, if the hash was added, <c>false</c> otherwise.</returns>
        /// <param name="tx">the transactions hash</param>
        public bool AddTransaction(Transaction tx)
        {
            if (Body == null)
                Body = new BlockBody();

            return Body.AddTransaction(tx);
        }

        /// <summary>
        /// Add transaction Hashes to the block
        /// </summary>
        /// <returns><c>true</c>, if the hash was added, <c>false</c> otherwise.</returns>
        /// <param name="txs">the transactions hash</param>
        public bool AddTransactions(IEnumerable<Hash> txs)
        {
            if (Body == null)
                Body = new BlockBody();

            return Body.AddTransactions(txs);
        }

        /// <summary>
        /// block signature
        /// </summary>
        /// <param name="keyPair"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void Sign(ECKeyPair keyPair)
        {
            ECSigner signer = new ECSigner();
            var hash = GetHash();
            var bytes = hash.DumpByteArray();
            ECSignature signature = signer.Sign(keyPair, bytes);

            Header.Sig = ByteString.CopyFrom(signature.SigBytes);
            Header.P = ByteString.CopyFrom(keyPair.PublicKey);
        }

        public ulong Index
        {
            get => Header?.Index ?? 0;
            set { }
        }

        public string BlockHashToHex
        {
            get => Header?.GetHash().ToHex() ?? Hash.Default.ToHex();
            set { }
        }

        public void FillTxsMerkleTreeRootInHeader()
        {
            Header.MerkleTreeRootOfTransactions = Body.CalculateMerkleTreeRoots();
        }

        public Hash GetHash()
        {
            return Header.GetHash();
        }

        public byte[] GetHashBytes()
        {
            return Header.GetHashBytes();
        }

        public void Complete(SideChainBlockInfo[] indexedSideChainBlockInfo = null, HashSet<TransactionResult> results = null)
        {
            if (results != null)
            {
                // add tx hash
                AddTransactions(results.Select(x => x.TransactionId));
                // set ws merkle tree root
                Header.MerkleTreeRootOfWorldState =
                    new BinaryMerkleTree().AddNodes(results.Select(x => x.StateHash)).ComputeRootHash();
            }
            
            Header.MerkleTreeRootOfTransactions = Body.CalculateMerkleTreeRoots();
            // Todo: improvement needed?
            Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);
            Body.Complete(Header.GetHash(), indexedSideChainBlockInfo);
        }

        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}
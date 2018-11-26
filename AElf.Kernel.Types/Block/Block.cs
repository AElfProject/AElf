using System;
using System.Collections.Generic;
using AElf.Cryptography.ECDSA;
using AElf.Common;
using Google.Protobuf;

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

            Header.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            Header.R = ByteString.CopyFrom(signature.R);
            Header.S = ByteString.CopyFrom(signature.S);
        }

        public ParentChainBlockInfo ParentChainBlockInfo { get; set; }

        public ulong Index
        {
            get => Header?.Index ?? 0;
            set { }
        }

        public string BlockHashToHex
        {
            get => Header?.GetHash().DumpHex() ?? Hash.Default.DumpHex();
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

        public Block Complete()
        {
            Header.MerkleTreeRootOfTransactions = Body.CalculateMerkleTreeRoots();
            Header.SideChainBlockHeadersRoot = Body.SideChainBlockHeadersRoot;
            Header.SideChainTransactionsRoot = Body.SideChainTransactionsRoot;
            Body.Complete(Header.GetHash());
            return this;
        }

        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}
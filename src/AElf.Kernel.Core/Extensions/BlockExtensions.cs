using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel
{
    public static class BlockExtensions
    {
        /// <summary>
        /// block signature
        /// </summary>
        /// <param name="keyPair"></param>
        /// <exception cref="NotImplementedException"></exception>
        public static void Sign(this IBlock block, byte[] publicKey, Func<byte[], Task<byte[]>> sign)
        {
            var hash = block.GetHash();
            var bytes = hash.DumpByteArray();
            var signature = sign(bytes).Result;

            block.Header.Sig = ByteString.CopyFrom(signature);
            block.Header.P = ByteString.CopyFrom(publicKey);
        }

        /// <summary>
        /// Add transaction Hashes to the block
        /// </summary>
        /// <returns><c>true</c>, if the hash was added, <c>false</c> otherwise.</returns>
        /// <param name="txs">the transactions hash</param>
        public static bool AddTransactions(this IBlock block, IEnumerable<Hash> txs)
        {
            if (block.Body == null)
                block.Body = new BlockBody();

            return block.Body.AddTransactions(txs);
        }

        /// <summary>
        /// Add transaction Hash to the block
        /// </summary>
        /// <returns><c>true</c>, if the hash was added, <c>false</c> otherwise.</returns>
        /// <param name="tx">the transactions hash</param>
        public static bool AddTransaction(this IBlock block, Transaction tx)
        {
            if (block.Body == null)
                block.Body = new BlockBody();

            return block.Body.AddTransaction(tx);
        }

        
        public static void FillTxsMerkleTreeRootInHeader(this IBlock block)
        {
            block.Header.MerkleTreeRootOfTransactions = block.Body.CalculateMerkleTreeRoots();
        }
    }
}
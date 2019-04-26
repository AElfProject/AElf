using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography;
using Google.Protobuf;

namespace AElf.Kernel
{
    public static class BlockExtensions
    {
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

        public static bool VerifyFormat(this IBlock block)
        {
            if (block.Header.Signature == null || block.Header.SignerKey == null)
                return false;
            if (block.Body.Transactions.Count == 0)
                return false;
            
            return true;
        }

        public static bool VerifySignature(this IBlock block)
        {
            if (!block.VerifyFormat())
                return false;

            var recoverResult = CryptoHelpers.RecoverPublicKey(block.Header.Signature.ToByteArray(), 
                                        block.GetHash().DumpByteArray(), out var recoveredPublicKey);
            return recoverResult && block.Header.SignerKey.ToByteArray().BytesEqual(recoveredPublicKey);
        }
    }
}
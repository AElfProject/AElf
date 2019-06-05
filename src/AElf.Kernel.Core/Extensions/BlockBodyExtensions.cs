using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel
{
    public static class BlockBodyExtensions
    {
        /// <summary>
        /// Calculate merkle tree root of transaction.
        /// </summary>
        /// <returns></returns>
        public static Hash CalculateMerkleTreeRoot(this BlockBody blockBody)
        {
            if (blockBody.TransactionsCount == 0)
                return Hash.Empty;
            var merkleTreeRoot = blockBody.Transactions.ComputeBinaryMerkleTreeRootWithLeafNodes();

            return merkleTreeRoot;
        }

        public static bool AddTransaction(this BlockBody blockBody, Transaction tx)
        {
            blockBody.Transactions.Add(tx.GetHash());
            return true;
        }

        public static bool AddTransactions(this BlockBody blockBody, IEnumerable<Hash> txs)
        {
            blockBody.Transactions.Add(txs);
            return true;
        }
    }
}
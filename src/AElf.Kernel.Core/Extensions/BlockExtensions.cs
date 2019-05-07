using AElf.Cryptography;
using AElf.Types;

namespace AElf.Kernel
{
    public static class BlockExtensions
    {
        /// <summary>
        /// Add transaction Hash to the block
        /// </summary>
        /// <returns><c>true</c>, if the hash was added, <c>false</c> otherwise.</returns>
        /// <param name="block"></param>
        /// <param name="tx">the transactions hash</param>
        public static void AddTransaction(this IBlock block, Transaction tx)
        {
            if (block.Body == null)
                block.Body = new BlockBody();

            block.Body.AddTransaction(tx);
        }

        public static bool VerifyFormat(this IBlock block)
        {
            if (block.Header.Signature.IsEmpty || block.Header.SignerPubkey.IsEmpty)
                return false;
            if (block.Body.Transactions.Count == 0)
                return false;

            return true;
        }

        public static bool VerifySignature(this IBlock block)
        {
            if (!block.VerifyFormat())
                return false;

            var recovered = CryptoHelpers.RecoverPublicKey(block.Header.Signature.ToByteArray(),
                                        block.GetHash().DumpByteArray(), out var publicKey);
            if (!recovered)
                return false;

            return block.Header.SignerPubkey.ToByteArray().BytesEqual(publicKey);
        }
    }
}
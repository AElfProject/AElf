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
            block.Body.AddTransaction(tx);
        }

        public static bool VerifySignature(this IBlock block)
        {
            if (!block.Header.VerifyFields() || !block.Body.VerifyFields())
                return false;

            if (block.Header.Signature.IsEmpty)
                return false;

            var recovered = CryptoHelper.RecoverPublicKey(block.Header.Signature.ToByteArray(),
                                        block.GetHash().ToByteArray(), out var publicKey);
            if (!recovered)
                return false;

            return block.Header.SignerPubkey.ToByteArray().BytesEqual(publicKey);
        }
    }
}
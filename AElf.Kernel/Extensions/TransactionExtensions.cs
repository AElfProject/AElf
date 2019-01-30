using System.Linq;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;

namespace AElf.Kernel
{
    public static class TransactionExtensions
    {
        public static int Size(this Transaction transaction)
        {
            return transaction.CalculateSize();
        }
        
        public static bool VerifySignature(this Transaction tx)
        {
            if (tx.Sigs == null || tx.Sigs.Count == 0)
            {
                return false;
            }

            if (tx.Sigs.Count == 1 && tx.Type != TransactionType.MsigTransaction)
            {
                var canBeRecovered = CryptoHelpers.RecoverPublicKey(tx.Sigs.First().ToByteArray(),
                    tx.GetHash().DumpByteArray(), out var pubKey);
                return canBeRecovered && Address.FromPublicKey(pubKey).Equals(tx.From);
            }
            
            return true;
        }
    }
}
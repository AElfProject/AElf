using System.Linq;
using AElf.Common;
using AElf.Cryptography;

namespace AElf.Kernel
{
    public static class TransactionExtensions
    {
        public static long GetExpiryBlockNumber(this Transaction transaction)
        {
            // TODO: Add ExpiryBlockNumber to Transaction
//            if (transaction.ExpiryBlockNumber != 0)
//            {
//                return transaction.ExpiryBlockNumber;
//            }

            return transaction.RefBlockNumber + KernelConstants.ReferenceBlockValidPeriod;
        }
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

            if (tx.Sigs.Count == 1)
            {
                var canBeRecovered = CryptoHelpers.RecoverPublicKey(tx.Sigs.First().ToByteArray(),
                    tx.GetHash().DumpByteArray(), out var pubKey);
                return canBeRecovered && Address.FromPublicKey(pubKey).Equals(tx.From);
            }
            
            return true;
        }
    }
}
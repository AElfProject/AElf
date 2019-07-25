using AElf.Cryptography;
using AElf.Types;

namespace AElf.Kernel
{
    public static class TransactionExtensions
    {
        public static long GetExpiryBlockNumber(this Transaction transaction)
        {
            return transaction.RefBlockNumber + KernelConstants.ReferenceBlockValidPeriod;
        }

        public static int Size(this Transaction transaction)
        {
            return transaction.CalculateSize();
        }

        public static bool VerifySignature(this Transaction transaction)
        {
            if (!transaction.VerifyFields())
                return false;

            var recovered = CryptoHelper.RecoverPublicKey(transaction.Signature.ToByteArray(), 
                transaction.GetHash().ToByteArray(), out var publicKey);

            if (!recovered)
                return false;

            return Address.FromPublicKey(publicKey).Equals(transaction.From);
        }
    }
}
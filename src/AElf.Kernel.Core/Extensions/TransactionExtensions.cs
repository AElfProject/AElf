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

        public static bool VerifyFormat(this Transaction transaction)
        {
            if (transaction.To == null || transaction.From == null)
                return false;
            if (transaction.To == transaction.From)
                return false;
            if (transaction.RefBlockNumber < 0)
                return false;
            if (string.IsNullOrEmpty(transaction.MethodName))
                return false;
            return true;
        }

        public static bool VerifySignature(this Transaction transaction)
        {
            if (!transaction.VerifyFormat())
                return false;

            var recovered = CryptoHelper.RecoverPublicKey(transaction.Signature.ToByteArray(), 
                transaction.GetHash().DumpByteArray(), out var publicKey);

            if (!recovered)
                return false;

            return Address.FromPublicKey(publicKey).Equals(transaction.From);
        }
    }
}
using System;
using System.Numerics;
using System.Text;
using System.Threading;

namespace AElf.Cryptography.SecretSharing
{
    public static class SecretSharingExtensions
    {
        /// <summary>
        /// convert the byte[] to BigInteger
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>BigInteger</returns>
        public static BigInteger ToBigInteger(this byte[] bytes)
        {
            const int bitSize = 8;

            var bitsSize = (bytes.Length + 1) * bitSize;
            var targetBytes = new byte[129];
            var filler = (int) SecretSharingConsts.MaxBits - bitsSize;
            Array.Copy(bytes, targetBytes, bytes.Length);
            Array.Copy(CryptoHelpers.RandomFill(filler / 8), 0, targetBytes, bytes.Length + 1, filler / 8);
            return new BigInteger(targetBytes);
        }

        public static string ConvertToString(this BigInteger integer)
        {
            var bytes = integer.ToByteArray();
            var chars = Encoding.UTF8.GetChars(bytes);
            var size = 0;
            for (; size < chars.Length; ++size)
            {
                if (chars[size] == '\0')
                {
                    break;
                }
            }

            return new string(chars, 0, size);
        }

        public static BigInteger Abs(this BigInteger integer)
        {
            return (integer % SecretSharingConsts.FieldPrime + SecretSharingConsts.FieldPrime) %
                   SecretSharingConsts.FieldPrime;
        }
    }
}
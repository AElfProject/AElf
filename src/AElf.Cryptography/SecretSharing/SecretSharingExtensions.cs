using System;
using System.Numerics;
using System.Text;

namespace AElf.Cryptography.SecretSharing
{
    public static class SecretSharingExtensions
    {
        public static BigInteger ToBigInteger(this string str)
        {
            const int bitSize = 8;

            var bitsSize = (str.Length + 1) * bitSize;
            var filler = (int) SecretSharingConsts.MaxBits - bitsSize;
            var totalBytes = new byte[129]; // 1024 / 8 + 1
            var strBytes = Encoding.UTF8.GetBytes(str);

            Array.Copy(strBytes, totalBytes, strBytes.Length);
            Array.Copy(CryptoHelpers.RandomFill(filler / 8), 0, totalBytes, strBytes.Length + 1, filler / 8);

            return new BigInteger(totalBytes);
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
            return (integer % SecretSharingConsts.FieldPrime + SecretSharingConsts.FieldPrime) % SecretSharingConsts.FieldPrime;
        }
    }
}
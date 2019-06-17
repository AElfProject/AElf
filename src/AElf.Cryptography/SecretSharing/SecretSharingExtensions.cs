using System;
using System.Numerics;
using System.Text;
using System.Threading;

namespace AElf.Cryptography.SecretSharing
{
    public static class SecretSharingExtensions
    {


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
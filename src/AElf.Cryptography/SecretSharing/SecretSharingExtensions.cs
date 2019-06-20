using System;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;

namespace AElf.Cryptography.SecretSharing
{
    public static class SecretSharingExtensions
    {

        public static BigInteger  ToBigInteger(this byte[] bytes)
        {
            var tempBytes = new byte[bytes.Length + 1];
            Array.Copy(bytes.Reverse().ToArray(), 0, tempBytes, 1, bytes.Length);
            return new BigInteger(tempBytes);
        }

        public static byte[] ToBytesArray(this BigInteger integer)
        {
            var tempBytes = integer.ToByteArray().Reverse().ToArray();
            var result = new byte[32];
            Array.Copy(tempBytes,0,result,0,result.Length);

            return result;
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
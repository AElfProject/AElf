using System;
using System.Linq;
using Org.BouncyCastle.Math;

namespace AElf.Cryptography.Core
{

    public static class Helpers
    {
        public static byte[] AddLeadingZeros(byte[] data, int requiredLength)
        {
            var zeroBytesLength = requiredLength - data.Length;
            if (zeroBytesLength <= 0) return data;
            var output = new byte[requiredLength];
            Buffer.BlockCopy(data, 0, output, zeroBytesLength, data.Length);
            for (int i = zeroBytesLength - 1; i >= 0; i--)
            {
                output[i] = 0x0;
            }

            return output;
        }


        public static byte[] Int2Bytes(BigInteger v, int rolen)
        {
            var result = v.ToByteArray();
            if (result.Length < rolen)
            {
                return AddLeadingZeros(result, rolen);
            }

            if (result.Length > rolen)
            {
                var skipLength = result.Length - rolen;
                return result.Skip(skipLength).ToArray();
            }

            return result;
        }

        public static BigInteger Bits2Int(byte[] inputBytes, int qlen)
        {
            var output = new BigInteger(1, inputBytes);
            if (inputBytes.Length * 8 > qlen)
            {
                return output.ShiftRight(inputBytes.Length * 8 - qlen);
            }

            return output;
        }

        public static byte[] Bits2Bytes(byte[] input, BigInteger q, int rolen)
        {
            var z1 = Bits2Int(input, q.BitLength);
            var z2 = z1.Subtract(q);
            if (z2.SignValue == -1)
            {
                return Int2Bytes(z1, rolen);
            }

            return Int2Bytes(z2, rolen);
        }
    }
}
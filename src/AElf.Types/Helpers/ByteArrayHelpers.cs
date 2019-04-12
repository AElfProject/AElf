using System;
using System.Linq;

namespace AElf.Common
{
    public static class ByteArrayHelpers
    {
        private static bool IsWithPrefix(string value)
        {
            return value.Length >= 2 && value[0] == '0' && (value[1] == 'x' || value[1] == 'X');
        }

        public static byte[] FromHexString(string hex)
        {
            if (IsWithPrefix(hex))
                hex = hex.Substring(2);
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];

            for (int i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            return bytes;
        }

        public static bool BytesEqual(this byte[] b1, byte[] b2)
        {
            if (b1 == b2)
                return true;

            if (b1 == null || b2 == null)
                return false;

            if (b1.Length != b2.Length)
                return false;

            for (var i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i]) return false;
            }

            return true;
        }

        
        
        public static byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays) {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
        
        public static byte[] ConcatArrays(params byte[][] arrays)
        {
            var result = new byte[arrays.Sum(arr => arr.Length)];
            var offset = 0;

            foreach (var arr in arrays)
            {
                Buffer.BlockCopy(arr, 0, result, offset, arr.Length);
                offset += arr.Length;
            }

            return result;
        }

        public static byte[] ConcatArrays(byte[] arr1, byte[] arr2)
        {
            var result = new byte[arr1.Length + arr2.Length];
            Buffer.BlockCopy(arr1, 0, result, 0, arr1.Length);
            Buffer.BlockCopy(arr2, 0, result, arr1.Length, arr2.Length);

            return result;
        }

        public static byte[] SubArray(byte[] arr, int start, int length)
        {
            var result = new byte[length];
            Buffer.BlockCopy(arr, start, result, 0, length);

            return result;
        }

        public static byte[] SubArray(byte[] arr, int start)
        {
            return SubArray(arr, start, arr.Length - start);
        }
    }
}
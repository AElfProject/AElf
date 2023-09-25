using System;
using System.Linq;

namespace AElf
{

    public static class NumericExtensions
    {
        public static byte[] ToBytes(this long n, bool bigEndian = true)
        {
            var bytes = BitConverter.GetBytes(n);
            return GetBytesWithEndian(bytes, bigEndian);
        }

        public static byte[] ToBytes(this ulong n, bool bigEndian = true)
        {
            var bytes = BitConverter.GetBytes(n);
            return GetBytesWithEndian(bytes, bigEndian);
        }

        public static byte[] ToBytes(this int n, bool bigEndian = true)
        {
            var bytes = BitConverter.GetBytes(n);
            return GetBytesWithEndian(bytes, bigEndian);
        }

        public static byte[] ToBytes(this uint n, bool bigEndian = true)
        {
            var bytes = BitConverter.GetBytes(n);
            return GetBytesWithEndian(bytes, bigEndian);
        }

        private static byte[] GetBytesWithEndian(byte[] bytes, bool bigEndian)
        {
            if (bigEndian ^ BitConverter.IsLittleEndian)
                return bytes;
            return bytes.Reverse().ToArray();
        }
    }
}
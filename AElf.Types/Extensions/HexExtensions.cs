using System;

namespace AElf.Common
{
    public static class HexExtensions
    {
        public static string ToHex(this long n)
        {
            return BitConverter.GetBytes(n).ToHex();
        }

        public static string ToHex(this int n)
        {
            return BitConverter.GetBytes(n).ToHex();
        }

        public static byte[] DumpByteArray(this int n)
        {
            return BitConverter.GetBytes(n);
        }
    }
}
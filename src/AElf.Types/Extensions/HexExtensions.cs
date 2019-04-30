using System;

namespace AElf
{
    public static class HexExtensions
    {
        public static byte[] DumpByteArray(this int n)
        {
            return BitConverter.GetBytes(n);
        }
    }
}
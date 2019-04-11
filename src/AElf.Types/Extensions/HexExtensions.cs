using System;

namespace AElf.Types
{
    public static class HexExtensions
    {
        public static byte[] DumpByteArray(this int n)
        {
            return BitConverter.GetBytes(n);
        }
    }
}
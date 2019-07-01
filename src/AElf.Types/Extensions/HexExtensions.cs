using System;

namespace AElf
{
    public static class HexExtensions
    {
        // Done: move to number extensions
        public static byte[] DumpByteArray(this int n)
        {
            return BitConverter.GetBytes(n);
        }
    }
}
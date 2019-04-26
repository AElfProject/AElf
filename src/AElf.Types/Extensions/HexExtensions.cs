using System;

namespace AElf
{
    public static class HexExtensions
    {
        // TODO: move to number extensions
        public static byte[] DumpByteArray(this int n)
        {
            return BitConverter.GetBytes(n);
        }
    }
}
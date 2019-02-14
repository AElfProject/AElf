using System;

namespace AElf.Common
{
    public static class HexExtensions
    {

        public static byte[] DumpByteArray(this int n)
        {
            return BitConverter.GetBytes(n);
        }
    }
}
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

        public static string DumpBase58(this int n)
        {
            // Default chain id is 4 base58 chars, bytes size is 3
            // sizeof(int) = 4, resize 3 bytes if the bytes[3] is 0
            var bytes = BitConverter.GetBytes(n);
            var lastIndex = Array.FindLastIndex(bytes, b => b != 0);
            if (lastIndex + 1 < 4)
                Array.Resize(ref bytes, lastIndex + 1);
            return bytes.ToPlainBase58();
        }

        public static byte[] DumpByteArray(this int n)
        {
            return BitConverter.GetBytes(n);
        }

        public static int ConvertBase58ToChainId(this string base58String)
        {
            // Use int type to save chain id (4 base58 chars, default is 3 bytes)
            var bytes = base58String.DecodeBase58();
            if (bytes.Length < 4)
                Array.Resize(ref bytes, 4);

            return BitConverter.ToInt32(bytes, 0);
        }
    }
}
using System;
using AElf.Common;

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
            return BitConverter.GetBytes(n).ToPlainBase58();
        }

        public static byte[] DumpByteArray(this int n)
        {
            return BitConverter.GetBytes(n);
        }
        
        public static int ConvertBase58ToChainId(this string b58str)
        {
            var bytes = b58str.DecodeBase58();
            if (bytes.Length < 4)
            {
                var n = 4 - bytes.Length;
                Array.Resize(ref bytes,4);
            }
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}
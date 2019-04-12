using System;
using System.Security.Cryptography;
using Google.Protobuf;

namespace AElf.Common
{
    public static class ByteExtensions
    {
        public static string ToPlainBase58(this byte[] value)
        {
            return Base58CheckEncoding.EncodePlain(value);
        }

        public static string ToPlainBase58(this ByteString value)
        {
            return Base58CheckEncoding.EncodePlain(value);
        }
        

        public static string ToHex(this byte[] bytes, bool withPrefix = false)
        {
            int offset = withPrefix ? 2 : 0;
            int length = bytes.Length * 2 + offset;
            char[] c = new char[length];

            byte b;

            if (withPrefix)
            {
                c[0] = '0';
                c[1] = 'x';
            }

            for (int bx = 0, cx = offset; bx < bytes.Length; ++bx, ++cx)
            {
                b = ((byte) (bytes[bx] >> 4));
                c[cx] = (char) (b > 9 ? b + 0x37 + 0x20 : b + 0x30);

                b = ((byte) (bytes[bx] & 0x0F));
                c[++cx] = (char) (b > 9 ? b + 0x37 + 0x20 : b + 0x30);
            }

            return new string(c);
        }

        public static string ToHex(this ByteString bytes, bool withPrefix = false)
        {
            int offset = withPrefix ? 2 : 0;
            int length = bytes.Length * 2 + offset;
            char[] c = new char[length];

            byte b;

            if (withPrefix)
            {
                c[0] = '0';
                c[1] = 'x';
            }

            for (int bx = 0, cx = offset; bx < bytes.Length; ++bx, ++cx)
            {
                b = ((byte) (bytes[bx] >> 4));
                c[cx] = (char) (b > 9 ? b + 0x37 + 0x20 : b + 0x30);

                b = ((byte) (bytes[bx] & 0x0F));
                c[++cx] = (char) (b > 9 ? b + 0x37 + 0x20 : b + 0x30);
            }

            return new string(c);
        }

        /// <summary>
        /// Calculates the hash for a byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] CalculateHash(this byte[] bytes)
        {
            return SHA256.Create().ComputeHash(bytes);
        }

        public static byte[] LeftPad(this byte[] bytes, int length)
        {
            if (length <= bytes.Length)
                return bytes;

            var paddedBytes = new byte[length];
            Buffer.BlockCopy(bytes, 0, paddedBytes, length - bytes.Length, bytes.Length);
            return paddedBytes;
        }
    }
}
using System;
using System.Linq;
using System.Security.Cryptography;

namespace AElf.Common
{
    public static partial class Extensions
    {
        public static string ToPlainBase58(this byte[] value)
        {
            return Base58CheckEncoding.EncodePlain(value);
        }

        public static string ToHex(this byte[] bytes, bool withPrefix=false)
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
        
        public static ulong ToUInt64(this byte[] bytes)
        {
            return BitConverter.ToUInt64(
                BitConverter.IsLittleEndian ? 
                    bytes.Reverse().ToArray() : 
                    bytes, 0);
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
    }
}
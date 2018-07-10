using System;

namespace AElf.Kernel
{
    public static class HexExtensions
    {
        public static string ToHex(this byte[] bytes)
        {
            char[] c = new char[bytes.Length * 2 + 2];

            byte b;

            c[0] = '0';
            c[1] = 'x';

            for (int bx = 0, cx = 2; bx < bytes.Length; ++bx, ++cx)
            {
                b = ((byte) (bytes[bx] >> 4));
                c[cx] = (char) (b > 9 ? b + 0x37 + 0x20 : b + 0x30);

                b = ((byte) (bytes[bx] & 0x0F));
                c[++cx] = (char) (b > 9 ? b + 0x37 + 0x20 : b + 0x30);
            }

            return new string(c);
        }

        public static byte[] HexToBytes(this string str)
        {
            //remove possible 0x prefix
            if (str.StartsWith("0x"))
            {
                str = str.Substring(2);
            }
            
            if (str.Length == 0 || str.Length % 2 != 0)
                return new byte[0];
            
            byte[] buffer = new byte[str.Length / 2];
            char c;
            for (int bx = 0, sx = 0; bx < buffer.Length; ++bx, ++sx)
            {
                // Convert first half of byte
                c = str[sx];
                buffer[bx] = (byte) ((c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0')) << 4);

                // Convert second half of byte
                c = str[++sx];
                buffer[bx] |= (byte) (c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0'));
            }

            return buffer;
        }
    }
}
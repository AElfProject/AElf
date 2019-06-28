using  Google.Protobuf;

namespace AElf
{
    public static class ByteStringExtensions
    {
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
    }
}
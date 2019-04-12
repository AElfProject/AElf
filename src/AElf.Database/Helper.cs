using System.Text;

namespace AElf.Database
{
    public static class Helper
    {
        public static int Memchr(byte[] bs, byte b, int offset)
        {
            for (var i = offset; i < bs.Length; i++)
            {
                if (bs[i] == b)
                {
                    return i;
                }
            }

            return -1;
        }

        public static string BytesToString(byte[] value)
        {
            if (value == null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(value);
        }

        public static byte[] StringToBytes(string value)
        {
            if (value == null)
            {
                return null;
            }

            return Encoding.UTF8.GetBytes(value);
        }
    }
}
using System.Text;

namespace AElf.Database.SsdbClient
{
    public static class Helper
    {
        public static bool BytesEqual(byte[] b1, byte[] b2)
        {
            if (b1 == b2) return true;
            if (b1 == null || b2 == null) return false;
            if (b1.Length != b2.Length) return false;
            for (var i=0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i]) return false;
            }
            return true;
        }
        
        public static int Memchr(byte[] bs, byte b, int offset) 
        {
            for(var i = offset; i < bs.Length; i++) 
            {
                if(bs[i] == b) 
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
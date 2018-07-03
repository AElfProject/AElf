using System;

namespace AElf.Common.ByteArrayHelpers
{
    public static class ByteArrayHelpers
    {
        public static byte[] FromHexString(string hex)
        {
            int numberChars = hex.Length - 2;
            byte[] bytes = new byte[numberChars / 2];
            
            for (int i = 2, j = 0 ; i < hex.Length; i += 2, j += 2)
                bytes[j / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            
            return bytes;
        }
        
        public static string ToHex(this byte[] bytes)
        {
            if (bytes.Length == 0)
                return "";
            char[] c = new char[2 + bytes.Length * 2];
            c[0] = '0';
            c[1] = 'x';
            byte b;

            for(int bx = 0, cx = 2; bx < bytes.Length; ++bx, ++cx) 
            {
                b = ((byte)(bytes[bx] >> 4));
                c[cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

                b = ((byte)(bytes[bx] & 0x0F));
                c[++cx]=(char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);
            }
            
            return new string(c);
        }
        
        public static bool BytesEqual(this byte[] b1, byte[] b2)
        {
            if (b1 == b2) 
                return true;
            
            if (b1 == null || b2 == null) 
                return false;
            
            if (b1.Length != b2.Length) 
                return false;
            
            for (var i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i]) return false;
            }

            return true;
        }
        
        public static byte[] RandomFill(int count)
        {
            Random rnd = new Random();
            byte[] random = new byte[count];
            
            rnd.NextBytes(random);

            return random;
        }
    }
}
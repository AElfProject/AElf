using System;

namespace AElf.Common.ByteArrayHelpers
{
    public static class ByteArrayHelpers
    {
        private static bool IsWithPrefix(string value)
        {
            return value.Length >= 2 && value[0] == '0' && (value[1] == 'x' || value[1] == 'X');
        }

        public static byte[] FromHexString(string hex)
        {
            if (IsWithPrefix(hex))
                hex = hex.Substring(2);
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];

            for (int i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            return bytes;
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
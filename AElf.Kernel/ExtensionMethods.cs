using System.Security.Cryptography;

namespace AElf.Kernel
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Reture the MD5 value for this string.
        /// </summary>
        /// <param name="str">this string.</param>
        /// <returns>corresponding MD5 value</returns>
        public static string GetMerkleHash(this string str)
        {
            string md5 = default(string);
            byte[] temp = MD5.Create().ComputeHash(System.Text.Encoding.ASCII.GetBytes(str));
            
            for (int i = 0; i < temp.Length; i++)
            {
                md5 += temp[i].ToString("x2");
            }
            return md5;
        }
    }
}

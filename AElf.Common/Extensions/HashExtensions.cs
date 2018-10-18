using System.Security.Cryptography;
using System.Text;

// ReSharper disable once CheckNamespace
namespace AElf.Common
{
    public static class HashExtensions
    {
        /// <summary>
        /// Calculates the hash for a byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] CalculateHash(this byte[] bytes)
        {
            return SHA256.Create().ComputeHash(bytes);
        }

        /// <summary>
        /// Calculates the hash for a string.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] CalculateHash(this string obj)
        {
            return CalculateHash(Encoding.UTF8.GetBytes(obj));
        }

        /// <summary>
        /// Checks if a <see cref="Hash"/> instance is null.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static bool IsNull(this Hash hash)
        {
            return hash == null || hash.DumpHex().RemoveHexPrefix().Length == 0;
        }
    }
}

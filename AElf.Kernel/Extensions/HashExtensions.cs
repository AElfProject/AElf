using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Kernel.Extensions
{
    public static class HashExtensions
    {
        public const int Length = 32;

        public static byte[] CalculateHash(this object obj)
        {
            if (obj == null)
            {
                return null;
            }
            return CalculateHash(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(obj)));
        }
        
        public static byte[] CalculateHashWith(this object obj, object another)
        {
            if (obj == null || another == null)
            {
                return null;
            }
            return CalculateHash(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(obj) + JsonConvert.SerializeObject(another)));
        }

        #region private methods
        /// <summary>
        /// Easier to change the implementation.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] CalculateHash(byte[] bytes)
        {
            return SHA256.Create().ComputeHash(bytes);
        }
        #endregion
    }
}

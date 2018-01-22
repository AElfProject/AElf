using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Kernel
{
    public static class HashingHelper
    {
        /// <summary>
        /// Why GetSHA256Hash?
        /// There could be other ways to get hash value like SHA3
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] GetSHA256Hash(this object obj)
        {
            return SHA256.Create().ComputeHash(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(obj)));
        }
    }
}
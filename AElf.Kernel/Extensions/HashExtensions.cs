using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using Google.Protobuf;

namespace AElf.Kernel.Extensions
{
    public static class HashExtensions
    {
        public static byte[] CalculateHash(this string obj)
        {
            return CalculateHash(Encoding.UTF8.GetBytes(obj));
        }
        
        public static byte[] CalculateHash(this IMessage obj)
        {
            return CalculateHash(obj.ToByteArray());
        }
        
        public static byte[] CalculateHashWith(this IMessage obj, IMessage another)
        {
            var bytes = new byte[obj.CalculateSize() + another.CalculateSize()];
            using (var stream = new CodedOutputStream(bytes))
            {
                obj.WriteTo(stream);
                another.WriteTo(stream);
                return CalculateHash(bytes);
            }
        }

        /// <summary>
        /// Use to calculate sub-DataProvider hash value.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] CalculateHashWith(this IMessage obj, string str)
        {
            var saltHash = CalculateHash(str);
            var bytes = obj.CalculateHash().Concat(saltHash).ToArray();
            return CalculateHash(bytes);
        }
        

        #region private methods
        /// <summary>
        /// Easier to change the implementation.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] CalculateHash(this byte[] bytes)
        {
            return SHA256.Create().ComputeHash(bytes);
        }
        #endregion
    }
}

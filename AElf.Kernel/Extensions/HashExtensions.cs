using System;
using System.Linq;
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
        /// Calculate hash value with a string.
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

        /// <summary>
        /// Quickly combine two hash values.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="another"></param>
        /// <returns></returns>
        public static Hash CombineHashWith(this Hash hash, Hash another)
        {
            if (another.Value.Length == 0)
            {
                return hash;
            }

            var length = hash.Value.Length;
            var newHashBytes = new byte[length];
            for (var i = 0; i < length; i++)
            {
                newHashBytes[i] = (byte) (hash.Value[i] ^ another.Value[i]);
            }

            return newHashBytes;
        }

        /// <summary>
        /// Provide another way to combine hash.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="another"></param>
        /// <returns></returns>
        public static Hash CombineReverseHashWith(this Hash hash, Hash another)
        {
            var reverse = hash.Value.Reverse().ToArray();
            if (another == null || another.Value.Length == 0)
            {
                return reverse;
            }

            if (hash.Value.Length == 0)
            {
                return another;
            }

            var length = Math.Min(reverse.Length, another.Value.Length);
            var newHashBytes = new byte[length];
            for (var i = 0; i < length; i++)
            {
                newHashBytes[i] = (byte) (reverse[i] ^ another.Value[i]);
            }

            return newHashBytes;
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

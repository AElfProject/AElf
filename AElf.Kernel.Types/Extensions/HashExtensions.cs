using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel
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
            if (another == null)
            {
                return (obj as Hash)?.Value.ToArray();
            }
            var bytes = new byte[obj.CalculateSize() + another.CalculateSize()];
            using (var stream = new CodedOutputStream(bytes))
            {
                obj.WriteTo(stream);
                another.WriteTo(stream);
                return CalculateHash(bytes);
            }
        }
        
        public static byte[] CalculateHashWith(this IMessage obj, ulong another)
        {
            var roundNumber = new UInt64Value {Value = another};
            var bytes = new byte[obj.CalculateSize() + roundNumber.CalculateSize()];
            using (var stream = new CodedOutputStream(bytes))
            {
                obj.WriteTo(stream);
                roundNumber.WriteTo(stream);
                return CalculateHash(bytes);
            }
        }
        
        public static byte[] CalculateHashWith(this IMessage obj, int another)
        {
            var roundNumber = new Int32Value() {Value = another};
            var bytes = new byte[obj.CalculateSize() + roundNumber.CalculateSize()];
            using (var stream = new CodedOutputStream(bytes))
            {
                obj.WriteTo(stream);
                roundNumber.WriteTo(stream);
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

            var length = Math.Min(hash.Value.Length, another.Value.Length);
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

        
        /// <summary>
        /// Easier to change the implementation.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] CalculateHash(this byte[] bytes)
        {
            return SHA256.Create().ComputeHash(bytes);
        }
        
        /// <summary>
        /// Calculate hash value of a hash list one by one
        /// </summary>
        /// <param name="hashes"></param>
        /// <returns></returns>
        public static Hash CalculateHashOfHashList(params Hash[] hashes)
        {
            if (hashes[0] == null)
            {
                throw new InvalidOperationException("Cannot calculate hash value with null.");
            }
            
            if (hashes.Length == 1)
            {
                return hashes[0];
            }
            
            var remains = hashes.Skip(1).ToArray();
            return hashes[0].CombineHashWith(CalculateHashOfHashList(remains));
        }
        
        public static bool IsNull(this Hash hash)
        {
            return hash == null || hash.ToHex().RemoveHexPrefix().Length == 0;
        }
    }
}

using AElf.Types;
using System;

namespace AElf
{
    public class HashHelper
    {
        public static Hash Base64ToHash(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            return Hash.LoadFrom(bytes);
        }

        /// <summary>
        /// Loads the content value represented in hex string.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Hash HexStringToHash(string hex)
        {
            var bytes = ByteArrayHelper.HexStringToByteArray(hex);
            return Hash.LoadFrom(bytes);
        }

        public static Hash ConcatAndCompute(Hash hash1, Hash hash2)
        {
            var bytes = ByteArrayHelper.ConcatArrays(hash1.ToByteArray(), hash2.ToByteArray());
            return Hash.ComputeFrom(bytes);
        }
        
        public static Hash ConcatAndCompute(Hash hash1, Hash hash2, Hash hash3)
        {
            var bytes = ByteArrayHelper.ConcatArrays(
                ByteArrayHelper.ConcatArrays(hash1.ToByteArray(), hash2.ToByteArray()), hash3.ToByteArray());
            return Hash.ComputeFrom(bytes);
        }
    }
}
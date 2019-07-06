using AElf.Types;
using System;

namespace AElf
{
    public class HashHelper
    {
        public static Hash LoadBase64ToHash(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            return Hash.FromByteArray(bytes);
        }
        
        /// <summary>
        /// Loads the content value represented in hex string.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Hash HexStringToHash(string hex)
        {
            var bytes = ByteArrayHelper.FromHexString(hex);
            return Hash.FromByteArray(bytes);
        }
    }
}
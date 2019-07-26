using AElf.Types;
using System;

namespace AElf
{
    public class HashHelper
    {
        public static Hash Base64ToHash(string base64)
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
            var bytes = ByteArrayHelper.HexStringToByteArray(hex);
            return Hash.FromByteArray(bytes);
        }

        /// <summary>
        /// Gets a new hash from two input hashes from bitwise xor operation.
        /// </summary>
        /// <param name="h1"></param>
        /// <param name="h2"></param>
        /// <returns></returns>
        public static Hash Xor(Hash h1, Hash h2)
        {
            var newBytes = new byte[TypeConsts.HashByteArrayLength];
            for (var i = 0; i < newBytes.Length; i++)
            {
                newBytes[i] = (byte) (h1.Value[i] ^ h2.Value[i]);
            }

            return Hash.FromRawBytes(newBytes);
        }
    }
}
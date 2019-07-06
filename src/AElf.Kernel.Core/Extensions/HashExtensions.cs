using System;
using AElf.Types;

namespace AElf.Kernel
{
    public static class HashExtensions
    {
        /// <summary>
        /// Checks if a <see cref="Hash"/> instance is null.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static bool IsNull(this Hash hash)
        {
            return hash == null || hash.ToHex().RemoveHexPrefix().Length == 0;
        }

        public static Hash Xor(this Hash hash, Hash another)
        {
            if (hash.Value.Length != another.Value.Length)
            {
                throw new InvalidOperationException("The two hashes don't have the same length");
            }

            var newBytes = new byte[hash.Value.Length];
            for (var i = 0; i < hash.Value.Length; ++i)
            {
                newBytes[i] = (byte) (hash.Value[i] ^ another.Value[i]);
            }

            return Hash.FromByteArray(newBytes);
        }
    }
}
using System;
using AElf.Types;

namespace AElf
{
    public static class HashExtensions
    {
        public static Hash ToHash(this int obj)
        {
            return Hash.FromRawBytes(BitConverter.GetBytes(obj));
        }
        
        // TODO: Consider Span
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

            return Hash.LoadByteArray(newBytes);
        }
    }
}
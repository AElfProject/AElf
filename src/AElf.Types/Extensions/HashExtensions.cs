using System;

namespace AElf
{
    public static class HashExtensions
    {
        public static Hash ComputeHash(this int obj)
        {
            return Hash.FromRawBytes(BitConverter.GetBytes(obj));
        }
        
        public static Hash Xor(this Hash hash, Hash another)
        {
            if (hash.Value.Length != another.Value.Length)
            {
                throw new InvalidOperationException("The two hashes don't have the same length");
            }

            Span<byte> newBytesSpan = new byte[hash.Value.Length];
            Span<byte> hashSpan = hash.Value.ToByteArray();
            Span<byte> anotherHashSpan = another.Value.ToByteArray();
            for (var i = 0; i < hash.Value.Length; ++i)
            {
                newBytesSpan[i] = (byte) (hashSpan[i] ^ anotherHashSpan[i]);
            }

            return Hash.LoadByteArray(newBytesSpan.ToArray());
        }
    }
}
using System;

namespace AElf
{
    public static class HashExtensions
    {
        // Done: rename ToHash
        public static Hash ComputeHash(this int obj)
        {
            return Hash.FromRawBytes(BitConverter.GetBytes(obj));
        }
    }
}
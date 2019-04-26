using System;

namespace AElf
{
    public static class HashExtensions
    {
        // TODO: rename ToHash
        public static Hash ComputeHash(this int obj)
        {
            return Hash.FromRawBytes(BitConverter.GetBytes(obj));
        }
    }
}
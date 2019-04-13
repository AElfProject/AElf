using System;

namespace AElf
{
    public static class HashExtensions
    {
        public static Hash ComputeHash(this int obj)
        {
            return Hash.FromRawBytes(BitConverter.GetBytes(obj));
        }
    }
}
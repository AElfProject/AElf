using System;

namespace AElf.Common
{
    public static class HashExtensions
    {
        public static Hash ComputeHash(this int obj)
        {
            return Hash.FromRawBytes(BitConverter.GetBytes(obj));
        }
    }
}
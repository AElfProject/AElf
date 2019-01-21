using System;

namespace AElf.Common
{
    public static class Extensions
    {
        public static Hash ComputeHash(this int obj)
        {
            return Hash.FromRawBytes(BitConverter.GetBytes(obj));
        }
    }
}
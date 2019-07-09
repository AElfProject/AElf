using System;
using AElf.Types;

namespace AElf
{
    public static class HashExtensions
    {
        public static Hash ToHash(this int intValue)
        {
            return Hash.FromRawBytes(BitConverter.GetBytes(intValue));
        }
    }
}
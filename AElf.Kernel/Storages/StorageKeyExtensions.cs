using System;
using AElf.Common;

namespace AElf.Kernel.Storages
{
    public static class StorageKeyExtensions
    {
        public static string ToStorageKey(this long n)
        {
            return BitConverter.GetBytes(n).ToHex();
        }
        public static string ToStorageKey(this int n)
        {
            return BitConverter.GetBytes(n).ToHex();
        }
        public static string ToStorageKey(this Hash hash)
        {
            return hash.ToHex();
        }
    }
}
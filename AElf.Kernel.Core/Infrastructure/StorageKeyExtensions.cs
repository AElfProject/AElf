using System;
using System.Linq;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel.Infrastructure
{
    public static class StorageKeyExtensions
    {
        public static string ToStorageKey(this long n)
        {
            return n.ToString();
        }
        
        public static string ToStorageKey(this ulong n)
        {
            return n.ToString();
        }
        public static string ToStorageKey(this int n)
        {
            return n.ToString();
        }
        public static string ToStorageKey(this Hash hash)
        {
            return hash?.Value.ToBase64();
        }
        
        public static string ToStorageKey(this ByteString byteString)
        {
            return byteString?.ToBase64();
        }
        
        public static string ToStorageKey(this Address byteString)
        {
            return byteString?.GetFormatted();
        }
    }
}
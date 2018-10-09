using System;
using AElf.Common.Extensions;
using Google.Protobuf;
using AElf.Common;

// ReSharper disable InconsistentNaming
namespace AElf.Kernel.Storages
{
    public static class Helper
    {
        public static string GetKeyString(this Hash hash, string type)
        {
            return new Key
            {
                Type = type,
                Value = ByteString.CopyFrom(hash.GetHashBytes()),
                HashType = (uint) hash.HashType
            }.ToByteArray().ToHex();
        }
    }
}
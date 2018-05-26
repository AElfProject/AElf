using System;
using System.Linq;

namespace AElf.Kernel.Extensions
{
    public static class ConvertExtensions
    {
        public static byte[] ToBytes(this ulong number)
        {
            return BitConverter.IsLittleEndian ? 
                BitConverter.GetBytes(number).Reverse().ToArray() : 
                BitConverter.GetBytes(number);
        }

        public static ulong ToUInt64(this byte[] bytes)
        {
            return BitConverter.ToUInt64(
                BitConverter.IsLittleEndian ? 
                    bytes.Reverse().ToArray() : 
                    bytes, 0);
        }
    }
}
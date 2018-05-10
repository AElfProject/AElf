using System;
using System.Linq;

namespace AElf.Kernel.Extensions
{
    public static class ConvertExtensions
    {
        public static byte[] ToBytes(this long number)
        {
            return BitConverter.IsLittleEndian ? 
                BitConverter.GetBytes(number).Reverse().ToArray() : 
                BitConverter.GetBytes(number);
        }

        public static long ToInt64(this byte[] bytes)
        {
            return BitConverter.ToInt64(
                BitConverter.IsLittleEndian ? 
                    bytes.Reverse().ToArray() : 
                    bytes, 0);
        }
    }
}
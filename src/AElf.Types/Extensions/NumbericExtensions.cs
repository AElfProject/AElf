using System;
using System.Linq;

namespace AElf
{
    public static class NumbericExtensions
    {
        public static byte[] ToBytes(this ulong number)
        {
            return BitConverter.IsLittleEndian ? BitConverter.GetBytes(number).Reverse().ToArray() : BitConverter.GetBytes(number);
        }
    }
}
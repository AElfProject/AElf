using System;
using System.Linq;

namespace AElf.Common
{
    public static partial class Extensions
    {
        public static byte[] ToBytes(this ulong number)
        {
            return BitConverter.IsLittleEndian ? 
                BitConverter.GetBytes(number).Reverse().ToArray() : 
                BitConverter.GetBytes(number);
        }
    }
}
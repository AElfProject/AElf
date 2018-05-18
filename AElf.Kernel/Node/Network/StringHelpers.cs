using System;
using System.Text;

namespace AElf.Kernel.Node.Network
{
    public static class StringHelpers
    {
        public static string ToUtf8(this Byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
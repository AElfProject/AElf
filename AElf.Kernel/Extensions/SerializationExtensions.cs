using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Kernel.Extensions
{
    public static class SerializationExtensions
    {
        public static byte[] ToBytes(this ISerializable obj)
        {
            // TODO:
            // Use this extension method to make the specific serialize implementation easy to change.
            throw new NotImplementedException();
        }

        public static ISerializable ToObject(this byte[] data)
        {
            // TODO:
            // Use this extension method to make the specific serialize implementation easy to change.
            throw new NotImplementedException();
        }
    }
}

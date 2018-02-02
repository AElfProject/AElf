using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Kernel.Extensions
{
    public static class SerializeExtensions
    {
        public static byte[] Serialize(this object obj)
        {
            // TODO:
            // Use this extension method to make the specific serialize implementation easy to change.
            throw new NotImplementedException();
        }

        public static object Deserialize(this ISerializable data)
        {
            // TODO:
            // Use this extension method to make the specific serialize implementation easy to change.
            throw new NotImplementedException();
        }
    }
}

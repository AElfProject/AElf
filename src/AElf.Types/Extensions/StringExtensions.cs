using System.Text;
using Google.Protobuf.Collections;

namespace AElf
{

    public static class StringExtensions
    {
        public static byte[] GetBytes(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        public static void MergeFrom<T1, T2>(this MapField<T1, T2> field, MapField<T1, T2> others)
        {
            field.Add(others);
        }
    }
}
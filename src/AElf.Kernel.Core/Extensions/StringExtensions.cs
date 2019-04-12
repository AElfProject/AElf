using System.Text;

namespace AElf.Kernel
{
    public static class StringExtensions
    {
        public static byte[] GetBytes(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }
    }
}
using System.Text;

namespace AElf.CSharp.Core.Utils
{
    public class EncodingHelper
    {
        public static byte[] GetBytesFromUtf8String(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }
    }
}
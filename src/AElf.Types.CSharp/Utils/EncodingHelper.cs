using System.Text;

namespace AElf.Types.CSharp.Utils
{
    public class EncodingHelper
    {
        public static byte[] GetBytesFromUtf8String(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }
    }
}
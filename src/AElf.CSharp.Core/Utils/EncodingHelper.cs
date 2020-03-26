using System.Text;

namespace AElf.CSharp.Core.Utils
{
    /// <summary>
    /// Helper class for serializing strings.
    /// </summary>
    public class EncodingHelper
    {
        /// <summary>
        /// Serializes a UTF-8 string to a byte array.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] GetBytesFromUtf8String(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }
    }
}
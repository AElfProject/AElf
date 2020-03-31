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
        /// <param name="str">the string to serialize.</param>
        /// <returns>the serialized string.</returns>
        public static byte[] EncodeUtf8(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }
    }
}
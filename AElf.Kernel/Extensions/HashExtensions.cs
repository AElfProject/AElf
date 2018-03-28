using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using Google.Protobuf;

namespace AElf.Kernel.Extensions
{
    public static class HashExtensions
    {
        public const int Length = 32;

        

        public static byte[] CalculateHash(this string obj)
        {
            return CalculateHash( Encoding.UTF8.GetBytes( obj ) );

        }
        
        public static byte[] CalculateHash(this IMessage obj)
        {
            return CalculateHash( obj.ToByteArray() );

        }
        
        public static byte[] CalculateHashWith(this IMessage obj, IMessage another)
        {
            var bytes = new byte[obj.CalculateSize() + another.CalculateSize()];
            var stream=new CodedOutputStream(bytes);
            obj.WriteTo(stream);
            another.WriteTo(stream);
            return CalculateHash(bytes);
        }

        #region private methods
        /// <summary>
        /// Easier to change the implementation.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] CalculateHash(this byte[] bytes)
        {
            return SHA256.Create().ComputeHash(bytes);
        }
        #endregion
    }
}

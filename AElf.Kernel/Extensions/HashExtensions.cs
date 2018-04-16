using System.Diagnostics;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using Google.Protobuf;

namespace AElf.Kernel.Extensions
{
    public static class HashExtensions
    {
        public static byte[] CalculateHash(this string obj)
        {
            return CalculateHash( Encoding.UTF8.GetBytes(obj));
        }
        
        public static byte[] CalculateHash(this IMessage obj)
        {
            return CalculateHash(obj.ToByteArray());
        }
        
        public static byte[] CalculateHashWith(this IMessage obj, IMessage another)
        {
            var bytes = new byte[obj.CalculateSize() + another.CalculateSize()];
            using (var stream = new CodedOutputStream(bytes))
            {
                obj.WriteTo(stream);
                another.WriteTo(stream);
                return CalculateHash(bytes);
            }
        }
        
        public static byte[] CalculateHash(this byte[] bytes)
        {
            return SHA256.Create().ComputeHash(bytes);
        }
    }
}

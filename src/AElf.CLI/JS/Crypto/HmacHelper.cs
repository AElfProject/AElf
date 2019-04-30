using System;
using System.Security.Cryptography;
using System.Text;
using ChakraCore.NET;
using ChakraCore.NET.API;

namespace AElf.CLI.JS.Crypto
{
    public class HmacHelper
    {
        public static string GetHmacDigest(JSValue algorithm, JSValue seed, JSValue data, JSValue format)
        {
            var hmac = GetHmac(algorithm, seed);
            AssertString(data);
            AssertString(format);
            return BitConverter
                .ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(string.Join("", data.ReferenceValue.ToString()))))
                .Replace("-", "").ToLower();
        }

        private static HMAC GetHmac(JSValue algorithm, JSValue seed)
        {
            AssertString(algorithm);
            AssertString(seed);

            var algo = algorithm.ReferenceValue.ToString().ToLower().Trim();
            switch (algo)
            {
                case "sha512":
                    return new HMACSHA512(Encoding.UTF8.GetBytes(seed.ReferenceValue.ToString()));
                case "sha256":
                    return new HMACSHA256(Encoding.UTF8.GetBytes(seed.ReferenceValue.ToString()));
                case "sha1":
                    return new HMACSHA1(Encoding.UTF8.GetBytes(seed.ReferenceValue.ToString()));
                case "sha384":
                    return new HMACSHA384(Encoding.UTF8.GetBytes(seed.ReferenceValue.ToString()));
                case "md5":
                    return new HMACMD5(Encoding.UTF8.GetBytes(seed.ReferenceValue.ToString()));
                default:
                    throw new Exception("Unknown algorithm.");
            }
        }

        private static void AssertString(JSValue value)
        {
            if (value.ReferenceValue.ValueType != JavaScriptValueType.String)
            {
                throw new Exception("Input is not string.");
            }
        }
    }
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Kernel.Extensions
{
    public static class HashExtensions
    {
        public static byte[] GetSHA256Hash(this object obj)
        {
            return SHA256.Create().ComputeHash(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(obj)));
        }
    }
}

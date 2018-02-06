using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AElf.Kernel.Extensions
{
    public static class HashExtensions
    {
        public static byte[] CalculateHash(this object obj)
        {
            return CalculateHash(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(obj)));
        }
        
        public static byte[] CalculateHashWith(this object obj, object another)
        {
            return CalculateHash(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(obj) + JsonConvert.SerializeObject(another)));
        }

        #region private methods
        private static byte[] CalculateHash(byte[] bytes)
        {
            return SHA256.Create().ComputeHash(bytes);
        }
        #endregion
    }
}

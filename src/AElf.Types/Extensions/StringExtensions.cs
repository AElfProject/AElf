﻿using System.Text;

namespace AElf
{
    public static class StringExtensions
    {
        public static string RemoveHexPrefix(this string hexStr)
        {
            return hexStr.StartsWith("0x") ? hexStr.Remove(0, 2) : hexStr;
        }

        public static string AppendHexPrefix(this string str)
        {
            return str.StartsWith("0x") ? str : "0x" + str;
        }

        /// <summary>
        /// Calculates the hash for a string.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] CalculateHash(this string obj)
        {
            return Encoding.UTF8.GetBytes(obj).CalculateHash();
        }

        public static byte[] DecodeBase58(this string value)
        {
            return Base58CheckEncoding.DecodePlain(value);
        }
    }
}
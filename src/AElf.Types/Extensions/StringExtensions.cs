﻿using System.Text;
using Google.Protobuf;

namespace AElf
{
    public static class StringExtensions
    {
        public static byte[] GetBytes(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }
    }
}
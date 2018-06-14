using System;
using System.Collections.Generic;

namespace AElf.ABI
{
    public static class NameExtension
    {
        public static string ToShorterName(this string longName)
        {
            Dictionary<string, string> shorterNames = new Dictionary<string, string>(){
                {"System.Void", "void"},
                {"System.Boolean", "bool"},
                {"System.Byte", "byte"},
                {"System.Byte[]", "byte[]"},
                {"System.Int32", "int"},
                {"System.UInt32", "uint"},
                {"System.Int64", "long"},
                {"System.UInt64", "ulong"},
                {"System.Object", "object"},
                {"System.String", "string"}
            };
            if (!shorterNames.TryGetValue(longName, out var shorterName))
            {
                shorterName = longName;
            }
            return shorterName;
        }
    }
}

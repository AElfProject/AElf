using System.Text.RegularExpressions;
using Org.BouncyCastle.Security;

namespace AElf.Kernel.SmartContract.MetaData
{
    public static class Replacement
    {
        private static string ReplacementRegexPattern = @"\$\{[a-zA-Z_][a-zA-Z0-9_]*((\.[a-zA-Z_][a-zA-Z0-9_]*)*)\}";
        
        public static readonly string This = "${this}";

        public static string Value(string replacement)
        {
            if (Regex.Match(replacement, ReplacementRegexPattern).Value.Equals(replacement))
            {
                return replacement.Substring(2, replacement.Length - 3);
            }
            else
            {
                throw new InvalidParameterException("The input value: " + replacement + "is not a replacement");
            }
        }
        
        public static string ReplaceValueIntoReplacement(string str, string replacement, string value)
        {
            return str.Replace(replacement, value);
        }
        
        public static bool TryGetReplacementWithIndex(string str, int index, out string res)
        {
            var replacements = Regex.Matches(str, ReplacementRegexPattern);
            if (index < replacements.Count)
            {
                res = replacements[index].Value;
                return true;
            }
            else
            {
                res = null;
                return false;
            }
        }
    }
}
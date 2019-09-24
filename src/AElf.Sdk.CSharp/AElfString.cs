namespace AElf.Sdk.CSharp
{
    public class AElfString
    {
        public static string Concat(string[] values)
        {
            return ValidatedString(string.Concat(values));
        }
        
        public static string Concat(object[] args)
        {
            return ValidatedString(string.Concat(args));
        }

        public static string Concat(string str0, string str1)
        {
            return ValidatedString(string.Concat(str0, str1));
        }

        public static string Concat(string str0, string str1, string str2)
        {
            return ValidatedString(string.Concat(str0, str1, str2));
        }
        
        public static string Concat(string str0, string str1, string str2, string str3)
        {
            return ValidatedString(string.Concat(str0, str1, str2, str3));
        }

        public static string Concat(object arg0, object arg1)
        {
            return ValidatedString(string.Concat(arg0, arg1));
        }
        
        public static string Concat(object arg0, object arg1, object arg2)
        {
            return ValidatedString(string.Concat(arg0, arg1, arg2));
        }
        
        public static string Concat(object arg0, object arg1, object arg2, object arg3)
        {
            return ValidatedString(string.Concat(arg0, arg1, arg2, arg3));
        }

        public static string ValidatedString(string str)
        {
            if (str.Length > 15360)
                throw new AssertionException($"String size {str.Length} is too big to concatenate further!");
            return str;
        }
    }
    
    public static class StringExtensions
    {
        public static string Append(this string s1, string s2)
        {
            return AElfString.ValidatedString(s1 + s2);
        }
        public static string AppendLine(this string s1, string s2)
        {
            return AElfString.ValidatedString(s1 + "\n" +  s2);
        }
    }
}

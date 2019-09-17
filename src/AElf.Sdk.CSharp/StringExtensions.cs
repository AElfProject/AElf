namespace AElf.Sdk.CSharp
{
    public static class StringExtensions
    {
        public static string Append(this string s1, string s2)
        {
            return Validated(s1 + s2);
        }
        public static string AppendLine(this string s1, string s2)
        {
            return Validated(s1 + "\n" +  s2);
        }

        static string Validated(string str)
        {
            if (str.Length > 10240)
                throw new AssertionException("String size is too big to append further!");
            return str;
        }
    }
}
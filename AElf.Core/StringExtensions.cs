namespace AElf
{
    public static class StringExtensions
    {
        /// <summary>
        /// Indicates whether this string is null or an System.String.Empty string.
        /// </summary>
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static string TrimEnd(this string str, string trimString)
        {
            if (str.EndsWith(trimString))
            {
                return str.Substring(0, str.Length - trimString.Length);
            }

            return str;
        }
    }
}
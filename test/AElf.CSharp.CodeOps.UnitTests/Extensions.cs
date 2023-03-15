using System.Text.RegularExpressions;

namespace AElf.CSharp.CodeOps;

public static class Extensions
{
    public static string CleanCode(this string originalCode)
    {
        var code = originalCode.Replace("\r\n", "\n");
        code = Regex.Replace(code, "\n+", "\n", RegexOptions.Multiline);
        return code.Trim();
    }
}
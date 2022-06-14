using System.Text.Json;

namespace AElf.WebApp.Web;

public class UpperCamelCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        var result = CamelCase.ConvertName(name);
        result = char.ToUpperInvariant(result[0]) + result[1..];
        return result;
    }
}
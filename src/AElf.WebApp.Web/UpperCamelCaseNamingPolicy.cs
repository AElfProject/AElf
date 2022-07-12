using System.Text.Json;

namespace AElf.WebApp.Web;

public class UpperCamelCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        var camelCase = CamelCase.ConvertName(name);
        return $"{char.ToUpperInvariant(camelCase[0])}{camelCase[1..]}";
    }
}
using Newtonsoft.Json.Serialization;

namespace AElf.WebApp.Web
{
    public class UpperCamelCaseNamingStrategy : CamelCaseNamingStrategy
    {
        protected override string ResolvePropertyName(string name)
        {
            var result = base.ResolvePropertyName(name);
            result = char.ToUpperInvariant(result[0]) + result.Substring(1);
            return result;
        }
    }

}
using System.Linq;

namespace Solang.Extensions
{
    public static class ABIExtensions
    {
        public static string? GetSelector(this SolangABI solangAbi, string methodName)
        {
            var selectorWithPrefix = methodName == "deploy"
                ? solangAbi.Spec.Constructors.First().Selector
                : solangAbi.Spec.Messages.FirstOrDefault(m => m.Label == methodName)?.Selector;
            return selectorWithPrefix?.Substring(selectorWithPrefix.StartsWith("0x") ? 2 : 0);
        }
    }
}
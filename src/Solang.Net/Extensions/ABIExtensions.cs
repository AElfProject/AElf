using System.Linq;

namespace Solang.Extensions
{
    public static class ABIExtensions
    {
        public static string GetSelector(this SolangABI solangAbi, string methodName)
        {
            var selectorWithPrefix = methodName == "deploy"
                ? solangAbi.Spec.Constructors.First().Selector
                : solangAbi.Spec.Messages.FirstOrDefault(m => m.Label == methodName)?.Selector;
            var selector = selectorWithPrefix?.Substring(selectorWithPrefix.StartsWith("0x") ? 2 : 0);
            if (selector == null)
            {
                throw new SelectorNotFoundException($"Selector of {methodName} not found.");
            }

            return selector;
        }
    }
}
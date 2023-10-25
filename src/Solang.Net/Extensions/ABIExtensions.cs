using System.Linq;

namespace Solang.Extensions
{
    public static class ABIExtensions
    {
        public static string GetSelector(this SolangABI solangAbi, string methodName)
        {
            var selectorWithPrefix = solangAbi.Spec.Messages.FirstOrDefault(m => m.Label == methodName)?.Selector;
            var selector = selectorWithPrefix?[(selectorWithPrefix.StartsWith("0x") ? 2 : 0)..];
            if (selector == null)
            {
                throw new SelectorNotFoundException($"Selector of {methodName} not found.");
            }

            return selector;
        }

        public static string GetConstructor(this SolangABI solangAbi)
        {
            var selectorWithPrefix = solangAbi.Spec.Constructors.First().Selector;
            var selector = selectorWithPrefix[(selectorWithPrefix.StartsWith("0x") ? 2 : 0)..];
            if (selector == null)
            {
                throw new SelectorNotFoundException($"Selector of constructor not found.");
            }

            return selector;
        }
    }
}
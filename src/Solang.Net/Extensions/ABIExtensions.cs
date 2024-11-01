using System;
using System.Linq;

namespace Solang.Extensions
{
    public static class ABIExtensions
    {
        public static string GetSelector(this SolangABI solangAbi, string methodName)
        {
            var selectorWithPrefix = (solangAbi.Spec.Messages.FirstOrDefault(m => m.Label == methodName)?.Selector ??
                                      solangAbi.Spec.Messages.FirstOrDefault(m => m.Label == $"{methodName}_")
                                          ?.Selector) ??
                                     solangAbi.Spec.Messages.FirstOrDefault(m => m.Label.StartsWith(methodName))
                                         ?.Selector;
            var selector = selectorWithPrefix?[(selectorWithPrefix.StartsWith("0x") ? 2 : 0)..];
            if (selector == null)
            {
                throw new SelectorNotFoundException($"Selector of {methodName} not found.");
            }

            return selector;
        }

        public static bool GetMutates(this SolangABI solangAbi, string selector)
        {
            var message = solangAbi.Spec.Messages.FirstOrDefault(m => m.Selector == $"0x{selector}");
            if (message == null)
            {
                throw new SelectorNotFoundException($"Method of selector {selector} not found.");
            }

            return message.Mutates;
        }
        
        public static string GetFunctionName(this SolangABI solangAbi, string selector)
        {
            var message = solangAbi.Spec.Messages.FirstOrDefault(m => m.Selector == $"0x{selector}");
            if (message == null)
            {
                throw new SelectorNotFoundException($"Method of selector {selector} not found.");
            }

            return message.Label;
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
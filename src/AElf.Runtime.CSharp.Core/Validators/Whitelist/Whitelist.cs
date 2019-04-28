using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace AElf.Runtime.CSharp.Validators.Whitelist
{
    public class Whitelist
    {
        private readonly IDictionary<string, NamespaceRule> _namespaces = new Dictionary<string, NamespaceRule>();

        public IReadOnlyDictionary<string, NamespaceRule> NameSpaces =>
            (IReadOnlyDictionary<string, NamespaceRule>) _namespaces;

        public Whitelist Namespace(string name, Permission permission,
            Action<NamespaceRule> namespaceRules = null)
        {
            var rule = new NamespaceRule(name, permission);

            _namespaces[name] = rule;

            namespaceRules?.Invoke(rule);

            return this;
        }

        public IEnumerable<WhitelistCheckResult> Check(MethodDefinition method, TypeReference type, string member = null)
        {
            throw new NotImplementedException();
        }
    }

    public enum WhitelistCheckResult
    {
        Allowed,
        DeniedNamespace,
        DeniedNametype,
        DeniedMember,
    }
}
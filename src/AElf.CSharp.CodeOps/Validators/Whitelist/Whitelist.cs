using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace AElf.CSharp.CodeOps.Validators.Whitelist
{
    public class Whitelist
    {
        private readonly IDictionary<string, Trust> _assemblies = new Dictionary<string, Trust>();
        private readonly IDictionary<string, NamespaceRule> _namespaces = new Dictionary<string, NamespaceRule>();

        public IReadOnlyDictionary<string, NamespaceRule> NameSpaces =>
            (IReadOnlyDictionary<string, NamespaceRule>) _namespaces;


        public Whitelist Assembly(System.Reflection.Assembly assembly, Trust trustLevel)
        {
            _assemblies.Add(assembly.GetName().Name, trustLevel);

            return this;
        }

        public Whitelist Namespace(string name, Permission permission,
            Action<NamespaceRule> namespaceRules = null)
        {
            var rule = new NamespaceRule(name, permission);

            _namespaces[name] = rule;

            namespaceRules?.Invoke(rule);

            return this;
        }

        public bool ContainsAssemblyNameReference(AssemblyNameReference assemblyNameReference)
        {
            return _assemblies.Keys.Contains(assemblyNameReference.Name);
        }

        public bool CheckAssemblyFullyTrusted(AssemblyNameReference assemblyNameReference)
        {
            return _assemblies.Any(asm => asm.Key == assemblyNameReference?.Name && asm.Value == Trust.Full);
        }

        public bool TryGetNamespaceRule(string typeNamespace, out NamespaceRule namespaceRule)
        {
            return _namespaces.TryGetValue(typeNamespace, out namespaceRule);
        }

        public bool ContainsWildcardMatchedNamespaceRule(string typeNamespace)
        {
            return _namespaces.Where(ns => ns.Value.Permission == Permission.Allowed
                                           && !ns.Value.Types.Any()
                                           && ns.Key.EndsWith("*"))
                .Any(ns => typeNamespace.StartsWith(ns.Key.Replace(".*", "")
                    .Replace("*", "")));
        }
    }
}
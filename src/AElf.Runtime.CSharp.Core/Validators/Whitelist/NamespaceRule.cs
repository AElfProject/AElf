using System;
using System.Collections.Generic;

namespace AElf.Runtime.CSharp.Validators.Whitelist
{
    public class NamespaceRule
    {
        public string Name { get; }
        public Permission Permission;

        private readonly IDictionary<string, TypeRule> _types = new Dictionary<string, TypeRule>();

        public IReadOnlyDictionary<string, TypeRule> Types => (IReadOnlyDictionary<string, TypeRule>) _types;

        public NamespaceRule(string name, Permission permission)
        {
            Name = name;
            Permission = permission;
        }

        public NamespaceRule Type(Type type, Permission permission, Action<TypeRule> typeRules = null)
        {
            return Type(type.Name, permission, typeRules);
        }

        public NamespaceRule Type(string name, Permission permission, Action<TypeRule> typeRules = null)
        {
            var rule = new TypeRule(name, permission);

            _types[name] = rule;
            
            typeRules?.Invoke(rule);

            return this;
        }
    }
}
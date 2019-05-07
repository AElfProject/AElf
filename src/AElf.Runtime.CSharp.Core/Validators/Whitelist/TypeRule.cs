using System.Collections.Generic;

namespace AElf.Runtime.CSharp.Validators.Whitelist
{
    public class TypeRule
    {
        public string Name { get; }
        public Permission Permission { get; }

        private readonly  IDictionary<string, MemberRule> _members = new Dictionary<string, MemberRule>();

        public IReadOnlyDictionary<string, MemberRule> Members => (IReadOnlyDictionary<string, MemberRule>) _members;

        public TypeRule(string name, Permission permission)
        {
            Name = name;
            Permission = permission;
        }

        public TypeRule Member(string name, Permission permission)
        {
            _members[name] = new MemberRule(name, permission);
            return this;
        }

        public TypeRule Constructor(Permission permission)
        {
            return Member(".ctor", permission);
        }
    }
}
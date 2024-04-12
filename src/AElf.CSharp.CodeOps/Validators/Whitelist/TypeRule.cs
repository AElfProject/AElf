using System.Collections.Generic;

namespace AElf.CSharp.CodeOps.Validators.Whitelist;

public class TypeRule
{
    private readonly IDictionary<string, MemberRule> _members = new Dictionary<string, MemberRule>();

    public TypeRule(string name, Permission permission)
    {
        Name = name;
        Permission = permission;
    }

    public string Name { get; }
    public Permission Permission { get; }

    public IReadOnlyDictionary<string, MemberRule> Members => (IReadOnlyDictionary<string, MemberRule>)_members;

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
namespace AElf.CSharp.CodeOps.Validators.Whitelist;

public class MemberRule
{
    public MemberRule(string name, Permission permission)
    {
        Name = name;
        Permission = permission;
    }

    public string Name { get; }
    public Permission Permission { get; }
}
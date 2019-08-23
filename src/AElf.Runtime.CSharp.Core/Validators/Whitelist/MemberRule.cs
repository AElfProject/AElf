namespace AElf.Runtime.CSharp.Validators.Whitelist
{
    public class MemberRule
    {
        public string Name { get; }
        public Permission Permission { get; }

        public MemberRule(string name, Permission permission)
        {
            Name = name;
            Permission = permission;
        }
    }
}
using AElf.CSharp.CodeOps.Validators.Whitelist;

namespace AElf.CSharp.CodeOps.Policies
{
    public class PrivilegePolicy : DefaultPolicy
    {
        public PrivilegePolicy()
        {
            Whitelist = Whitelist.Namespace("System.Threading", Permission.Allowed);
        }
    }
}

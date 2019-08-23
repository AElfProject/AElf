using AElf.Runtime.CSharp.Validators.Whitelist;

namespace AElf.Runtime.CSharp.Policies
{
    public class PrivilegePolicy : DefaultPolicy
    {
        public PrivilegePolicy()
        {
            Whitelist = Whitelist.Namespace("System.Threading", Permission.Allowed);
        }
    }
}

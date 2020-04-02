using System.Linq;
using System.Reflection;
using AElf.CSharp.CodeOps.Validators.Module;

namespace AElf.CSharp.CodeOps.Policies
{
    public class SystemPolicy : DefaultPolicy
    {
        public SystemPolicy()
        {
            // Exclude ObserverProxyValidator
            var observerValidator = ModuleValidators.Single(m => m is ObserverProxyValidator);
            ModuleValidators.Remove(observerValidator);
        }
    }
}
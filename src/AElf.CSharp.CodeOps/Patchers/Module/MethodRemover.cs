using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace AElf.CSharp.CodeOps.Patchers.Module
{
    public class MethodRemover : IPatcher<ModuleDefinition>
    {
        private static HashSet<string> _methodsToRemove = new HashSet<string>
        {
            "GetHashCode"
        };

        public void Patch(ModuleDefinition module)
        {
            foreach (var typ in module.Types)
            {
                PatchType(typ);
            }
        }

        public void PatchType(TypeDefinition typ)
        {
            var methodsToRemove = typ.Methods.Where(m => 
                _methodsToRemove.Contains(m.Name)).ToList();
            
            foreach (var method in methodsToRemove)
            {
                typ.Methods.Remove(method);
            }
            
            foreach (var nestedType in typ.NestedTypes)
            {
                PatchType(nestedType);
            }
        }
    }
}
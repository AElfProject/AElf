using System.IO;
using AElf.CSharp.CodeOps.Patchers;
using AElf.CSharp.CodeOps.Patchers.Module;
using Mono.Cecil;

namespace AElf.CSharp.CodeOps
{
    public static class ContractPatcher
    {
        private static readonly IPatcher<ModuleDefinition>[] ModulePatchers = {
            new ExecutionObserverInjector(),
            new MethodCallReplacer()
        };

        public static byte[] Patch(byte[] code)
        {
            var assemblyDef = AssemblyDefinition.ReadAssembly(new MemoryStream(code));

            foreach (var modulePatcher in ModulePatchers)
            {
                modulePatcher.Patch(assemblyDef.MainModule);
            }
            
            var newCode = new MemoryStream();
            assemblyDef.Write(newCode);
            return newCode.ToArray();
        }
    }
}

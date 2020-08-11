using System.IO;
using System.Linq;
using AElf.CSharp.CodeOps.Policies;
using AElf.Kernel.CodeCheck.Infrastructure;
using Mono.Cecil;

namespace AElf.CSharp.CodeOps
{
    public class CSharpContractPatcher : IContractPatcher
    {
        private readonly IPolicy _policy;

        public CSharpContractPatcher(IPolicy policy)
        {
            _policy = policy;
        }


        public byte[] Patch(byte[] code, bool isSystemContract)
        {
            var assemblyDef = AssemblyDefinition.ReadAssembly(new MemoryStream(code));
            Patch(assemblyDef.MainModule, isSystemContract);
            var newCode = new MemoryStream();
            assemblyDef.Write(newCode);
            return newCode.ToArray();
        }

        public int Category => 0;
        
        private void Patch<T>(T t, bool isSystemContract)
        {
            var patchers = _policy.GetPatchers<T>().Where(p => !p.SystemContactIgnored || !isSystemContract).ToList();
            patchers.ForEach(v => v.Patch(t));
        }
    }
}

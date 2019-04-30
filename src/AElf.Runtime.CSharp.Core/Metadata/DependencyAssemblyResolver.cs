using System.IO;
using Mono.Cecil;

namespace AElf.Runtime.CSharp.Metadata
{
    public class DependencyAssemblyResolver : DefaultAssemblyResolver
    {
        public DependencyAssemblyResolver(string path)
        {
            var files = Directory.GetFiles(path, "*.dll");
            foreach (var f in files)
            {
                RegisterAssembly(AssemblyDefinition.ReadAssembly(f));
            }
        }
    }
}
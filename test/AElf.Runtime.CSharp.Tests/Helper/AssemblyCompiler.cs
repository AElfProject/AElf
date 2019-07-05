using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;

namespace AElf.Runtime.CSharp.Helper
{
    public static class AssemblyCompiler
    {
        private static readonly Assembly[] References = {
            Assembly.Load("System.Runtime"),
            typeof(object).Assembly
        };

        public static AssemblyDefinition Compile(string assemblyName, string code)
        {
            var compilation = CSharpCompilation.Create(
                assemblyName,
                new [] { CSharpSyntaxTree.ParseText(code)},
                References.Select(r => MetadataReference.CreateFromFile(r.Location)),
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    checkOverflow: true,
                    optimizationLevel: OptimizationLevel.Release,
                    deterministic: true)
            );
            
            var dllStream = new MemoryStream();
            var emitResult = compilation.Emit(dllStream);

            if (!emitResult.Success)
                return null;
            
            dllStream.Position = 0;

            return AssemblyDefinition.ReadAssembly(dllStream);
        }
    }
}

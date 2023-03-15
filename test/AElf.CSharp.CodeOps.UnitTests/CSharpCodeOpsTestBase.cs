using System.Text;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AElf.CSharp.CodeOps;

public class CSharpCodeOpsTestBase
{
    protected static TypeDefinition FindType(ModuleDefinition module, string name)
    {
        return module.GetAllTypes().Single(t => t.Name == name);
    }

    protected static MethodDefinition FindMethod(TypeDefinition typ, string name)
    {
        return typ.Methods.Single(x => x.Name == name);
    }

    protected AssemblyDefinition CompileToAssemblyDefinition(string source)
    {
        var bytes = CompileAssembly(source);
        var stream = new MemoryStream(bytes);
        var asm = AssemblyDefinition.ReadAssembly(stream);
        var module = asm.MainModule;
        foreach (var methodDefinition in module.GetAllTypes().SelectMany(t => t.Methods))
        {
            if (methodDefinition.HasBody)
            {
                // Make sure the body has been initialized as the stream will be closed after returning
                var _ = methodDefinition.Body.Instructions;
            }
        }

        return asm;
    }

    protected byte[] CompileAssembly(string source)
    {
        var tree = SyntaxFactory.ParseSyntaxTree(source.Trim());
        var compilation = CSharpCompilation.Create("__Code__")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release))
            //.WithReferences(Basic.Reference.Assemblies.Net60.All)   // NUGET Package for all framework references
            .WithReferences(References)
            .AddSyntaxTrees(tree);

        string errorMessage = null;

        using (var codeStream = new MemoryStream())
        {
            // Actually compile the code
            EmitResult compilationResult = null;
            compilationResult = compilation.Emit(codeStream);

            // Compilation Error handling
            if (!compilationResult.Success)
            {
                var sb = new StringBuilder();
                foreach (var diag in compilationResult.Diagnostics)
                {
                    sb.AppendLine(diag.ToString());
                }

                errorMessage = sb.ToString();

                throw new Exception(errorMessage);
            }

            return codeStream.ToArray();
        }
    }

    public static HashSet<PortableExecutableReference> References { get; set; } =
        new HashSet<PortableExecutableReference>();

    static CSharpCodeOpsTestBase()
    {
        AddNetCoreDefaultReferences();
        AddSmartContractReferences();
    }

    public static bool AddAssembly(string assemblyDll)
    {
        if (string.IsNullOrEmpty(assemblyDll)) return false;

        var file = Path.GetFullPath(assemblyDll);

        if (!File.Exists(file))
        {
            // check framework or dedicated runtime app folder
            var path = Path.GetDirectoryName(typeof(object).Assembly.Location);
            file = Path.Combine(path, assemblyDll);
            if (!File.Exists(file))
                return false;
        }

        if (References.Any(r => r.FilePath == file)) return true;

        try
        {
            var reference = MetadataReference.CreateFromFile(file);
            References.Add(reference);
        }
        catch
        {
            return false;
        }

        return true;
    }

    public static void AddAssemblies(params string[] assemblies)
    {
        foreach (var file in assemblies)
            AddAssembly(file);
    }


    public static void AddNetCoreDefaultReferences()
    {
        var rtPath = Path.GetDirectoryName(typeof(object).Assembly.Location) +
                     Path.DirectorySeparatorChar;

        AddAssemblies(
            rtPath + "System.Private.CoreLib.dll",
            rtPath + "System.Runtime.dll",
            rtPath + "System.Console.dll",
            rtPath + "netstandard.dll",
            rtPath + "System.Text.RegularExpressions.dll", // IMPORTANT!
            rtPath + "System.Linq.dll",
            rtPath + "System.Linq.Expressions.dll", // IMPORTANT!
            rtPath + "System.IO.dll",
            rtPath + "System.Net.Primitives.dll",
            rtPath + "System.Net.Http.dll",
            rtPath + "System.Private.Uri.dll",
            rtPath + "System.Reflection.dll",
            rtPath + "System.ComponentModel.Primitives.dll",
            rtPath + "System.Globalization.dll",
            rtPath + "System.Collections.Concurrent.dll",
            rtPath + "System.Collections.NonGeneric.dll",
            rtPath + "Microsoft.CSharp.dll"
        );
    }

    private static void AddSmartContractReferences()
    {
        AddAssembly(typeof(CSharpSmartContract).Assembly.Location);
        AddAssembly(typeof(IMessage).Assembly.Location);
    }
}
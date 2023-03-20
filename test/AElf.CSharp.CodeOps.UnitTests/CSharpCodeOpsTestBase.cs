using System.Text;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
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

    protected static string DecompileType(TypeDefinition typ)
    {
        using var stream = new MemoryStream();
        typ.Module.Assembly.Write(stream);

        stream.Seek(0, SeekOrigin.Begin);
        var peFile = new PEFile("Contract", stream);
        var resolver = new UniversalAssemblyResolver(
            typeof(ISmartContract).Assembly.Location, true, ".NETSTANDARD"
        );
        resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(object).Assembly.Location));
        var typeSystem = new DecompilerTypeSystem(peFile, resolver);
        var decompiler = new CSharpDecompiler(typeSystem, new DecompilerSettings());
        var fullName = typ.FullName.Replace("/", "+");
        var syntaxTree = decompiler.DecompileType(new FullTypeName(fullName));
        var decompiled = syntaxTree.ToString().Replace("\r\n", "\n");
        return decompiled;
    }

    public static HashSet<PortableExecutableReference> References { get; set; } =
        new HashSet<PortableExecutableReference>();

    static CSharpCodeOpsTestBase()
    {
        AddNet6References();
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


    public static void AddNet6References()
    {
        foreach (var portableExecutableReference in Basic.Reference.Assemblies.Net60.References.All)
        {
            References.Add(portableExecutableReference);
        }
    }

    private static void AddSmartContractReferences()
    {
        AddAssembly(typeof(CSharpSmartContract).Assembly.Location);
        AddAssembly(typeof(IMessage).Assembly.Location);
        AddAssembly(typeof(Address).Assembly.Location);
        AddAssembly(typeof(AElf.CSharp.Core.IMethod).Assembly.Location);
        AddAssembly(typeof(AElf.Kernel.SmartContract.IExecutionObserver).Assembly.Location);
    }
}
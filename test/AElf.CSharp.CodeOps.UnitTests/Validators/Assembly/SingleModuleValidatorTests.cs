using AElf.CSharp.CodeOps.Validators.Assembly;
using Mono.Cecil;
using Xunit;

namespace AElf.CSharp.CodeOps.UnitTests.Validators.Assembly;

public class SingleModuleValidatorTests : CSharpCodeOpsTestBase
{
    [Fact]
    public void Check_Passes_If_Assembly_Contains_Only_One_Module()
    {
        var assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("TestAssembly", new Version(1, 0, 0, 0)),
            "TestAssembly.dll",
            ModuleKind.Dll);
        var type1 = new TypeDefinition("Namespace1", "Class1", TypeAttributes.Public | TypeAttributes.Class,
            assembly.MainModule.TypeSystem.Object);
        assembly.MainModule.Types.Add(type1);

        var errorMessages = new SingleModuleValidator().Validate(assembly, CancellationToken.None)
            .Select(r => r.Message).ToList();

        Assert.Empty(errorMessages);
    }

    [Fact]
    public void Check_Fails_If_Assembly_Contains_More_Than_One_Module()
    {
        var assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("TestAssembly", new Version(1, 0, 0, 0)),
            "TestAssembly.dll",
            ModuleKind.Dll);
        var type1 = new TypeDefinition("Namespace1", "Class1", TypeAttributes.Public | TypeAttributes.Class,
            assembly.MainModule.TypeSystem.Object);
        assembly.MainModule.Types.Add(type1);

        var module2 = ModuleDefinition.CreateModule("Module2", ModuleKind.NetModule);
        var type2 = new TypeDefinition("Namespace2", "Class2", TypeAttributes.Public | TypeAttributes.Class,
            module2.TypeSystem.Object);
        module2.Types.Add(type2);

        assembly.Modules.Add(module2);
        var errorMessages = new SingleModuleValidator().Validate(assembly, CancellationToken.None)
            .Select(r => r.Message).ToList();

        Assert.Single(errorMessages);
        Assert.Contains("Only one module is allowed in the assembly.", errorMessages.Single());
    }
}
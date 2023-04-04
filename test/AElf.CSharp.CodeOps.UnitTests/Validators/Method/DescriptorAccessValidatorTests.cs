using Google.Protobuf.Reflection;

namespace AElf.CSharp.CodeOps.Validators.Method;

using System.Linq;
using Xunit;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Threading;


public class DescriptorAccessValidatorTests
{
    private readonly DescriptorAccessValidator _validator;

    public DescriptorAccessValidatorTests()
    {
        _validator = new DescriptorAccessValidator();
    }

    [Fact]
    public void TestNoBodyMethod() // No body method should be valid.
    {
        var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
        var method = new MethodDefinition("NoBodyMethod", MethodAttributes.Public, module.TypeSystem.Void);

        var result = _validator.Validate(method, CancellationToken.None).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void TestValidFileDescriptorAccess() // Valid file descriptor access should be valid.
    {
        var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
        var method = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
        var declaringType = new TypeDefinition("TestNamespace", "TestType", TypeAttributes.Public);
        var field = new FieldDefinition("fileDescriptor", FieldAttributes.Static | FieldAttributes.Public, module.ImportReference(typeof(FileDescriptor)));
        declaringType.Fields.Add(field);
        method.DeclaringType = declaringType;

        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, field));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Stsfld, field));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        var result = _validator.Validate(method, CancellationToken.None).ToList();

        Assert.Empty(result);
    }


    [Fact]
    public void TestInvalidFileDescriptorAccess() // Invalid file descriptor access should be invalid.
    {
        var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
        var method = new MethodDefinition("InvalidFileDescriptorAccess", MethodAttributes.Public, module.TypeSystem.Void);
        var declaringType = new TypeDefinition("TestNamespace", "TestType", TypeAttributes.Public);
        var field = new FieldDefinition("fileDescriptor", FieldAttributes.Static | FieldAttributes.Public, module.ImportReference(typeof(FileDescriptor)));
        declaringType.Fields.Add(field);
        method.DeclaringType = declaringType;

        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, field));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Stsfld, field));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        var result = _validator.Validate(method, CancellationToken.None).ToList();

        Assert.Single(result);
        Assert.IsType<DescriptorAccessValidationResult>(result.First());
        Assert.Contains("It is not allowed to set FileDescriptor type static field outside of its declaring type's constructor.", result.First().Message);
    }

    [Fact]
    public void TestInvalidFileDescriptorAccessInConstructor() // Invalid file descriptor access in constructor should be invalid.
    {
        var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
        var method = new MethodDefinition(".ctor", MethodAttributes.Public, module.TypeSystem.Void);
        var declaringType = new TypeDefinition("TestNamespace", "TestType", TypeAttributes.Public);
        var field = new FieldDefinition("fileDescriptor", FieldAttributes.Static | FieldAttributes.Public, module.ImportReference(typeof(FileDescriptor)));
        var otherType = new TypeDefinition("TestNamespace", "OtherType", TypeAttributes.Public);
        otherType.Fields.Add(field);
        method.DeclaringType = declaringType;

        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, field));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Stsfld, field));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        var result = _validator.Validate(method, CancellationToken.None).ToList();

        Assert.Single(result);
        Assert.IsType<DescriptorAccessValidationResult>(result.First());
       
        Assert.Single(result);
        Assert.IsType<DescriptorAccessValidationResult>(result.First());
        Assert.Contains("It is not allowed to set FileDescriptor type static field outside of its declaring type's constructor.", result.First().Message);
    }
}

using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace AElf.CSharp.CodeOps.Validators.Method;

public class GetHashCodeValidatorTests
{
    private readonly GetHashCodeValidator _validator;

    public GetHashCodeValidatorTests()
    {
        _validator = new GetHashCodeValidator();
    }

    [Fact]
    public void TestGetHashCodeMethodWithInvalidCall()
    {
        var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
        var classType = new TypeDefinition("TestNamespace", "TestClass", TypeAttributes.Public, module.TypeSystem.Object);
        module.Types.Add(classType);

        var invalidMethodDef = new MethodDefinition("InvalidMethod", MethodAttributes.Public, module.TypeSystem.Void);
        classType.Methods.Add(invalidMethodDef);
        var invalidMethod = new MethodReference("InvalidMethod", module.TypeSystem.Void, classType);

        var method = new MethodDefinition(nameof(GetHashCode), MethodAttributes.Public, module.TypeSystem.Int32);
        classType.Methods.Add(method);
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, invalidMethod));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        var result = _validator.Validate(method, CancellationToken.None).ToList();

        Assert.Single(result);
        Assert.IsType<GetHashCodeValidationResult>(result.First());
        Assert.Contains("It is not allowed to access InvalidMethod method within GetHashCode method.", result.First().Message);
    }


    [Fact]
    public void TestGetHashCodeMethodWithValidCall()
    {
        var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
        var classType = new TypeDefinition("TestNamespace", "TestClass", TypeAttributes.Public, module.TypeSystem.Object);
        module.Types.Add(classType);
        var validMethod = new MethodReference(nameof(GetHashCode), module.TypeSystem.Int32, classType);
        var method = new MethodDefinition(nameof(GetHashCode), MethodAttributes.Public, module.TypeSystem.Int32);
        classType.Methods.Add(method);
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, validMethod));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        var result = _validator.Validate(method, CancellationToken.None).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void TestMethodWithInvalidFieldAccessInGetHashCode()
    {
        var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
        var classType = new TypeDefinition("TestNamespace", "TestClass", TypeAttributes.Public, module.TypeSystem.Object);
        module.Types.Add(classType);
        var field = new FieldDefinition("InvalidField", FieldAttributes.Public, module.TypeSystem.Int32);
        classType.Fields.Add(field);
        var method = new MethodDefinition(nameof(GetHashCode), MethodAttributes.Public, module.TypeSystem.Int32);
        classType.Methods.Add(method);
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, field));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        var result = _validator.Validate(method, CancellationToken.None).ToList();

        Assert.Single(result);
        Assert.IsType<GetHashCodeValidationResult>(result.First());
        Assert.Contains("It is not allowed to set InvalidField field within GetHashCode method.", result.First().Message);
    }
}

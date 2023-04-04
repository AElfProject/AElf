using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElf.CSharp.CodeOps.Validators.Method;

public class ArrayValidatorTest
{
    private ArrayValidator _validator;
    private ModuleDefinition _module;

    public ArrayValidatorTest()
    {
        _validator = new ArrayValidator();
        _module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
    }

    private TypeReference VoidTypeReference()
    {
        return _module.ImportReference(typeof(void));
    }

    [Fact]
    public void TestEmptyMethod()
    {
        var method = new MethodDefinition("EmptyMethod", MethodAttributes.Public, VoidTypeReference());

        var result = _validator.Validate(method, CancellationToken.None).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void TestValidArraySize()
    {
        var method = new MethodDefinition("ValidArraySize", MethodAttributes.Public, VoidTypeReference());
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 1024));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, _module.ImportReference(typeof(byte))));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 10 * 1024 / 4));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, _module.ImportReference(typeof(int))));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 5));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, _module.ImportReference(typeof(string))));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 4));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, _module.ImportReference(typeof(Type))));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 3));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, _module.ImportReference(typeof(object))));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        var result = _validator.Validate(method, CancellationToken.None).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void TestInvalidArraySize()
    {
        var method = new MethodDefinition("InvalidArraySize", MethodAttributes.Public, _module.TypeSystem.Void);
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 1024 * 41));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, _module.ImportReference(typeof(byte))));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 10 * 1024 / 2));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, _module.ImportReference(typeof(int))));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 10));

        method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, _module.ImportReference(typeof(string))));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 6));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, _module.ImportReference(typeof(Type))));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        var result = _validator.Validate(method, CancellationToken.None).ToList();

        Assert.NotEmpty(result);
        Assert.True(result.Count > 0);
        Assert.All(result, validationResult => Assert.IsType<ArrayValidationResult>(validationResult));
    }

    [Fact]
    public void TestCancellationRequested()
    {
        var method = new MethodDefinition("CancellationRequested", MethodAttributes.Public, VoidTypeReference());
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_5));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, _module.ImportReference(typeof(int))));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Assert.Throws<ContractAuditTimeoutException>(() => _validator.Validate(method, cancellationTokenSource.Token));
    }
}

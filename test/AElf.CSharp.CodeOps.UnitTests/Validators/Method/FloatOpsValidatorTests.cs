using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace AElf.CSharp.CodeOps.Validators.Method;

public class FloatOpsValidatorTests
{
    private readonly FloatOpsValidator _validator;

    public FloatOpsValidatorTests()
    {
        _validator = new FloatOpsValidator();
    }

    [Fact]
    public void TestMethodWithoutFloatOps()
    {
        var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
        var method = new MethodDefinition("TestMethod", MethodAttributes.Public, module.TypeSystem.Void);
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 42));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        var result = _validator.Validate(method, CancellationToken.None).ToList();

        Assert.Empty(result);
    }
    
    [Fact]
    public void TestMethodWithFloatOps()
    {
        var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
        var method = new MethodDefinition("TestMethod", MethodAttributes.Public, module.TypeSystem.Void);
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_R4, 3.14f));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        var validator = new FloatOpsValidator();
        var result = validator.Validate(method, CancellationToken.None).ToList();

        Assert.Single(result);
        Assert.IsType<FloatOpsValidationResult>(result.First());
        Assert.Contains("Method TestMethod contains ldc.r4 float OpCode.", result.First().Message);
    }


}

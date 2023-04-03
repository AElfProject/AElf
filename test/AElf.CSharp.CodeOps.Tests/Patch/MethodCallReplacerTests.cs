using System.Linq;
using AElf.CSharp.CodeOps.Patchers.Module;
using AElf.CSharp.CodeOps.Patchers.Module.SafeMethods;
using AElf.Runtime.CSharp.Tests.TestContract;
using AElf.Sdk.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Shouldly;
using Xunit;

namespace AElf.CSharp.CodeOps.Patch;

public class MethodCallReplacerTests : CSharpCodeOpsTestBase
{
    [Fact]
    public void MethodCallReplacer_StringConcat_Test()
    {
        var module = GetContractModule(typeof(TestContract));
        {
            var m = module.Types.Single(t => t.Name == nameof(TestContractReflection)).GetStaticConstructor();
            var res = ContainsMethodCall(m,
                GetModule(typeof(AElfString)).Types.Single(t => t.FullName == typeof(AElfString).FullName).Methods
                    .First(m => m.Name == nameof(string.Concat)));
            res.ShouldBeFalse();
        }

        var replacer = new StringMethodsReplacer();
        replacer.Patch(module);

        {
            var m = module.Types.Single(t => t.Name == nameof(TestContractReflection)).GetStaticConstructor();
            var res = ContainsMethodCall(m,
                GetModule(typeof(AElfString)).Types.Single(t => t.FullName == typeof(AElfString).FullName).Methods
                    .First(m => m.Name == nameof(string.Concat)));
            res.ShouldBeTrue();
        }
    }

    [Fact]
    public void MethodCallReplacer_MathOperator_Test()
    {
        var module = GetContractModule(typeof(TestContract));
        {
            var res = ContainsMathOpCode(
                GetModule(typeof(TestContract)).Types.Single(t => t.FullName == typeof(TestContract).FullName)
                    .Methods
                    .First(m => m.Name == nameof(TestContract.TestStateType)), OpCodes.Add);
            res.ShouldBeTrue();
        }

        var replacer = new StringMethodsReplacer();
        replacer.Patch(module);
        var safeMathPatcher = new Patchers.Module.SafeMath.Patcher();
        safeMathPatcher.Patch(module);

        {
            var res = ContainsMathOpCode(
                module.Types.Single(t => t.FullName == typeof(TestContract).FullName)
                    .Methods
                    .First(m => m.Name == nameof(TestContract.TestStateType)), OpCodes.Add);
            res.ShouldBeFalse();
        }

        {
            var res = ContainsMathOpCode(
                module.Types.Single(t => t.FullName == typeof(TestContract).FullName)
                    .Methods
                    .First(m => m.Name == nameof(TestContract.TestStateType)), OpCodes.Add_Ovf);
            res.ShouldBeTrue();
        }
    }

    private bool ContainsMethodCall(MethodDefinition methodDefinition, MethodDefinition expected)
    {
        return methodDefinition.Body.Instructions.Any(i =>
            i.Operand is MethodReference method && method.DeclaringType.FullName == expected.DeclaringType.FullName &&
            method.Name == expected.Name);
    }

    private bool ContainsMathOpCode(MethodDefinition methodDefinition, OpCode opCode)
    {
        return methodDefinition.Body.Instructions.Any(i => i.OpCode == opCode);
    }
}
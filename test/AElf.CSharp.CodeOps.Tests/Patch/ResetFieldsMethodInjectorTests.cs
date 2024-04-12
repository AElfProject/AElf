using System.Linq;
using AElf.CSharp.CodeOps.Patchers.Module;
using AElf.Runtime.CSharp.Tests.TestContract;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Shouldly;
using Xunit;

namespace AElf.CSharp.CodeOps.Patch;

public class ResetFieldsMethodInjectorTests : CSharpCodeOpsTestBase
{
    [Fact(Skip = "Static field not allowed in user code https://github.com/AElfProject/AElf/issues/3388")]
    public void ResetFields_Static_Test()
    {
        var module = GetContractModule(typeof(TestContract));
        {
            var typeDefinition = module.Types.Single(t => t.Name == nameof(TestContract));
            var resetMethodExists = typeDefinition.Methods.Any(m => m.Name == "ResetFields");
            resetMethodExists.ShouldBeFalse();
        }

        {
            var typeDefinition = module.Types.Single(t => t.Name == nameof(TestContract));
            var nestTypeDefinition =
                typeDefinition.NestedTypes.Single(t => t.Name == nameof(TestContract.TestNestClass));
            var resetMethodExists = nestTypeDefinition.Methods.Any(m => m.Name == "ResetFields");
            resetMethodExists.ShouldBeFalse();
        }

        {
            var typeDefinition = module.Types.Single(t => t.Name == nameof(TestContractState));
            var resetMethodExists = typeDefinition.Methods.Any(m => m.Name == "ResetFields");
            resetMethodExists.ShouldBeFalse();
        }

        var resetMethodInjector = new ResetFieldsMethodInjector();
        resetMethodInjector.Patch(module);

        {
            var typeDefinition = module.Types.Single(t => t.Name == nameof(TestContract));
            var resetMethodExists = typeDefinition.Methods.Any(m => m.Name == "ResetFields");
            resetMethodExists.ShouldBeTrue();
            var method = typeDefinition.Methods.Single(m => m.Name == "ResetFields");
            var instructions = method.Body.Instructions;
            {
                var resetFiled = instructions.Any(i =>
                    i.OpCode == OpCodes.Stsfld && i.Operand is FieldDefinition fieldDefinition &&
                    fieldDefinition.Name == "i");
                resetFiled.ShouldBeTrue();
            }
        }

        {
            var typeDefinition = module.Types.Single(t => t.Name == nameof(TestContract));
            var nestTypeDefinition =
                typeDefinition.NestedTypes.Single(t => t.Name == nameof(TestContract.TestNestClass));
            var resetMethodExists = nestTypeDefinition.Methods.Any(m => m.Name == "ResetFields");
            resetMethodExists.ShouldBeTrue();
            var method = typeDefinition.Methods.Single(m => m.Name == "ResetFields");
            var instructions = method.Body.Instructions;

            {
                var resetFiled = instructions.Any(i =>
                    i.OpCode == OpCodes.Stsfld && i.Operand is FieldDefinition fieldDefinition &&
                    fieldDefinition.Name == "i");
                resetFiled.ShouldBeTrue();
            }

            {
                var resetFiled = instructions.Any(i =>
                    i.OpCode == OpCodes.Stfld && i.Operand is FieldDefinition fieldDefinition &&
                    fieldDefinition.Name == "j");
                resetFiled.ShouldBeFalse();
            }
        }

        {
            var typeDefinition = module.Types.Single(t => t.Name == nameof(TestContractState));
            var resetMethodExists = typeDefinition.Methods.Any(m => m.Name == "ResetFields");
            resetMethodExists.ShouldBeTrue();
            var method = typeDefinition.Methods.Single(m => m.Name == "ResetFields");
            var instructions = method.Body.Instructions;

            {
                var resetFiled = instructions.Any(i =>
                    i.OpCode == OpCodes.Stsfld && i.Operand is FieldDefinition fieldDefinition &&
                    fieldDefinition.Name == "boolState");
                resetFiled.ShouldBeTrue();
            }

            {
                var resetFiled = instructions.Any(i =>
                    i.OpCode == OpCodes.Stsfld && i.Operand is FieldDefinition fieldDefinition &&
                    fieldDefinition.Name == "boolState2");
                resetFiled.ShouldBeFalse();
            }
        }
    }
}
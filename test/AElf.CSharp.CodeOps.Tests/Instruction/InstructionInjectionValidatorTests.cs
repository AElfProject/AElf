using System.Threading;
using AElf.CSharp.CodeOps.Instructions;
using AElf.CSharp.CodeOps.Validators.Method;
using AElf.Runtime.CSharp.Tests.TestContract;
using Shouldly;
using Xunit;

namespace AElf.CSharp.CodeOps.Instruction;

public class InstructionInjectionValidatorTests : CSharpCodeOpsTestBase
{
    [Fact]
    public void TestInstructionInjectionTest()
    {
        // not patched
        {
            var instructionInjectionValidator =
                new InstructionInjectionValidator(new StateWrittenInstructionInjector());
            var tokenContractModule = GetContractModule(typeof(TestContract));
            var validationResults =
                instructionInjectionValidator.Validate(tokenContractModule, CancellationToken.None);
            validationResults.ShouldContain(v => v.Info.Type == typeof(TestContract).FullName);
            // validationResults.ShouldContain(v => v.Info.Type.Contains(nameof(TestContract.TestNestClass)));
        }

        // patched
        {
            var instructionInjectionValidator =
                new InstructionInjectionValidator(new StateWrittenInstructionInjector());
            var tokenContractModule = GetPatchedContractModule(typeof(TestContract));
            var validationResults =
                instructionInjectionValidator.Validate(tokenContractModule, CancellationToken.None);
            validationResults.ShouldBeEmpty();
        }
    }
}
using System.Linq;
using AElf.CSharp.CodeOps.Instructions;
using AElf.Runtime.CSharp.Tests.TestContract;
using AElf.Sdk.CSharp;
using AElf.Types;
using Mono.Cecil;
using Shouldly;
using Xunit;

namespace AElf.CSharp.CodeOps.Instruction
{
    public class StateWrittenInstructionInjectorTests : CSharpCodeOpsTestBase
    {
        private readonly StateWrittenInstructionInjector _stateWrittenInstructionInjector;

        public StateWrittenInstructionInjectorTests()
        {
            _stateWrittenInstructionInjector = GetRequiredService<StateWrittenInstructionInjector>();
        }

        [Fact]
        public void IdentifyInstruction_Tests()
        {
            var testContractModule = GetContractModule(typeof(TestContract));
            var tokenContractTypeDef = testContractModule.GetType("AElf.Runtime.CSharp.Tests.TestContract", nameof(TestContract));
            var createMethod = tokenContractTypeDef.Methods.First(m => m.Name == nameof(TestContract.TestStateType));

            var identifyResult = createMethod.Body.Instructions
                .Where(i => _stateWrittenInstructionInjector.IdentifyInstruction(i)).ToList();
            identifyResult.Count.ShouldBe(3);
            {
                var methodReference = (MethodReference) identifyResult[0].Operand;
                ((GenericInstanceType) methodReference.DeclaringType).GenericArguments.Last().Resolve().FullName
                    .ShouldBe(typeof(ProtobufMessage).FullName);
            }
            
            {
                var methodReference = (MethodReference) identifyResult[1].Operand;
                ((GenericInstanceType) methodReference.DeclaringType).GenericArguments.Last().Resolve().FullName
                    .ShouldBe(typeof(Address).FullName);
            }
            
            {
                var methodReference = (MethodReference) identifyResult[2].Operand;
                ((GenericInstanceType) methodReference.DeclaringType).GenericArguments.Last().Resolve().FullName
                    .ShouldBe(typeof(string).FullName);
            }
        }


        [Fact]
        public void InjectInstruction_Tests()
        {
            var tokenContractModule = GetContractModule(typeof(TestContract));
            var tokenContractTypeDef =
                tokenContractModule.GetType("AElf.Runtime.CSharp.Tests.TestContract", nameof(TestContract));
            var createMethod = tokenContractTypeDef.Methods.First(m => m.Name == nameof(TestContract.TestStateType));

            var identifyResult = createMethod.Body.Instructions
                .Where(i => _stateWrittenInstructionInjector.IdentifyInstruction(i)).ToList();

            var ilProcessor = createMethod.Body.GetILProcessor();
            var instructionToInject = identifyResult.First();
            _stateWrittenInstructionInjector.InjectInstruction(ilProcessor, instructionToInject, tokenContractModule);

            var callInstruction = instructionToInject.Previous;
            ((MethodReference) callInstruction.Operand).Name.ShouldBe(nameof(CSharpSmartContractContext
                .ValidateStateSize));
        }

        [Fact]
        public void ValidateInstruction_Tests()
        {
            var tokenContractModule = GetContractModule(typeof(TestContract));
            var tokenContractTypeDef =
                tokenContractModule.GetType("AElf.Runtime.CSharp.Tests.TestContract", nameof(TestContract));
            var createMethod = tokenContractTypeDef.Methods.First(m => m.Name == nameof(TestContract.TestStateType));

            var identifyResult = createMethod.Body.Instructions
                .Where(i => _stateWrittenInstructionInjector.IdentifyInstruction(i)).ToList();

            var ilProcessor = createMethod.Body.GetILProcessor();
            var instructionToInject = identifyResult.First();

            {
                var validationResult =
                    _stateWrittenInstructionInjector.ValidateInstruction(tokenContractModule, instructionToInject);
                validationResult.ShouldBeFalse();
            }
            _stateWrittenInstructionInjector.InjectInstruction(ilProcessor, instructionToInject, tokenContractModule);
            {
                var validationResult =
                    _stateWrittenInstructionInjector.ValidateInstruction(tokenContractModule, instructionToInject);
                validationResult.ShouldBeTrue();
            }
        }
    }
}
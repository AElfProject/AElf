using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.CodeOps.Instructions;
using AElf.Runtime.CSharp.Tests.TestContract;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;
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
            var tokenContractModule = GetModule(typeof(TestContract));
            var tokenContractTypeDef = tokenContractModule.GetType("AElf.Runtime.CSharp.Tests.TestContract", nameof(TestContract));
            var createMethod = tokenContractTypeDef.Methods.First(m => m.Name == nameof(TestContract.TestStateType));

            var identifyResult = createMethod.Body.Instructions
                .Where(i => _stateWrittenInstructionInjector.IdentifyInstruction(i)).ToList();
            identifyResult.Count.ShouldBe(2);
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
        }
    }
}
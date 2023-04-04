using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace AElf.CSharp.CodeOps.Validators.Method;

public class UncheckedMathValidatorTests
    {
        private readonly UncheckedMathValidator _validator;

        public UncheckedMathValidatorTests()
        {
            _validator = new UncheckedMathValidator();
        }    

        [Fact]
        public void TestMethodWithoutUncheckedMath()
        {
            var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
            var classType = new TypeDefinition("TestNamespace", "TestClass", TypeAttributes.Public, module.TypeSystem.Object);
            module.Types.Add(classType);
            var method = new MethodDefinition("TestMethod", MethodAttributes.Public, module.TypeSystem.Void);
            classType.Methods.Add(method);

            method.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            var result = _validator.Validate(method, CancellationToken.None).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void TestMethodWithAddition()
        {
            TestMethodWithUncheckedMath(OpCodes.Add);
        }

        [Fact]
        public void TestMethodWithSubtraction()
        {
            TestMethodWithUncheckedMath(OpCodes.Sub);
        }

        [Fact]
        public void TestMethodWithMultiplication()
        {
            TestMethodWithUncheckedMath(OpCodes.Mul);
        }

        private void TestMethodWithUncheckedMath(OpCode opCode)
        {
            var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
            var classType = new TypeDefinition("TestNamespace", "TestClass", TypeAttributes.Public, module.TypeSystem.Object);
            module.Types.Add(classType);
            var method = new MethodDefinition("TestMethod", MethodAttributes.Public, module.TypeSystem.Void);
            classType.Methods.Add(method);

            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            method.Body.Instructions.Add(Instruction.Create(opCode));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            var result = _validator.Validate(method, CancellationToken.None).ToList();

            Assert.Single(result);
            Assert.IsType<UncheckedMathValidationResult>(result.First());
            Assert.Contains($"TestMethod contains unsafe OpCode {opCode}", result.First().Message);
        }

    }
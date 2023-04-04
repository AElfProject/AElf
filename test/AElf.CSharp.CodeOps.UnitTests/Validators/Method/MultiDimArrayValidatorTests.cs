using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Xunit;


namespace AElf.CSharp.CodeOps.Validators.Method;

public class MultiDimArrayValidatorTests
    {
        private readonly MultiDimArrayValidator _validator;

        public MultiDimArrayValidatorTests()
        {
            _validator = new MultiDimArrayValidator();
        }

        [Fact]
        public void TestMethodWithoutBody()
        {
            var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
            var classType = new TypeDefinition("TestNamespace", "TestClass", TypeAttributes.Public, module.TypeSystem.Object);
            module.Types.Add(classType);
            var method = new MethodDefinition("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract, module.TypeSystem.Void);
            classType.Methods.Add(method);

            var result = _validator.Validate(method, CancellationToken.None).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void TestMethodWithoutMultiDimArray()
        {
            var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
            var classType = new TypeDefinition("TestNamespace", "TestClass", TypeAttributes.Public, module.TypeSystem.Object);
            module.Types.Add(classType);
            var method = new MethodDefinition("TestMethod", MethodAttributes.Public, module.TypeSystem.Void);
            classType.Methods.Add(method);
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, module.TypeSystem.Int32));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            var result = _validator.Validate(method, CancellationToken.None).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void TestMethodWithMultiDimArray()
        {
            var module = ModuleDefinition.CreateModule("TestModule", ModuleKind.Dll);
            var classType = new TypeDefinition("TestNamespace", "TestClass", TypeAttributes.Public, module.TypeSystem.Object);
            module.Types.Add(classType);
            var method = new MethodDefinition("TestMethod", MethodAttributes.Public, module.TypeSystem.Void);
            classType.Methods.Add(method);

            var intArrayType = new ArrayType(module.TypeSystem.Int32);
            var intArrayArrayType = new ArrayType(intArrayType);

            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, intArrayArrayType));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            var result = _validator.Validate(method, CancellationToken.None).ToList();

            Assert.Single(result);
            Assert.IsType<MultiDimArrayValidationResult>(result.First());
            Assert.Contains("TestMethod contains multi dimension array declaration.", result.First().Message);
        }

    }
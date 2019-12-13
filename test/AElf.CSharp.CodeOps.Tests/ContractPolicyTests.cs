using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Contracts.Genesis;
using AElf.CSharp.CodeOps.Policies;
using AElf.CSharp.CodeOps.Validators;
using AElf.CSharp.CodeOps.Validators.Method;
using AElf.CSharp.CodeOps.Validators.Whitelist;
using AElf.Runtime.CSharp.Tests.BadContract;
using Mono.Cecil;
using Shouldly;
using Xunit;

namespace AElf.CSharp.CodeOps
{
    public class FirstPolicy : AbstractPolicy
    {
        public FirstPolicy()
        {
            Whitelist = new Whitelist();
            Whitelist.Namespace("System.Text", Permission.Allowed);
            Whitelist.Namespace("System.Random", Permission.Denied);

            MethodValidators.AddRange(new IValidator<MethodDefinition>[]
            {
                new ArrayValidator(),
                new FloatOpsValidator(),
            });
        }
    }

    public class SecondPolicy : AbstractPolicy
    {
        public SecondPolicy()
        {
            Whitelist = new Whitelist();
            Whitelist.Namespace("System.Linq", Permission.Allowed);
            Whitelist.Namespace("System.DateTime", Permission.Denied);

            MethodValidators.AddRange(new IValidator<MethodDefinition>[]
            {
                new GenericParamValidator(),
                new MultiDimArrayValidator(),
            });
        }
    }

    public class ThirdPolicy : AbstractPolicy
    {
        public ThirdPolicy()
        {
            Whitelist = new Whitelist();
            Whitelist.Namespace("System.Collections", Permission.Allowed);
            Whitelist.Namespace("System.OS", Permission.Denied);

            MethodValidators.AddRange(new IValidator<MethodDefinition>[]
            {
                new NewObjValidator(),
                new UncheckedMathValidator()
            });
        }
    }

    public class MultiplePolicies : AbstractPolicy
    {
        public MultiplePolicies(List<AbstractPolicy> policies)
            : base(policies)
        {
            Whitelist = new Whitelist();
            policies.ForEach(o => this.Whitelist.NameSpaces.Concat(o.Whitelist.NameSpaces));
        }
    }

    public class ContractPolicyTests : CSharpCodeOpsTestBase
    {
        private ContractAuditor _auditor;
        private readonly string _contractDllDir = "../../../contracts/";
        private readonly byte[] _systemContractCode;
        private readonly byte[] _badContractCode;

        public ContractPolicyTests()
        {
            _systemContractCode = ReadCode(_contractDllDir + typeof(BasicContractZero).Module);
            _badContractCode = ReadCode(_contractDllDir + typeof(BadContract).Module);
        }

        [Fact]
        public void Multiple_Policy_Test()
        {
            var multiplePolicies = new MultiplePolicies(new List<AbstractPolicy>
            {
                new FirstPolicy(),
                new SecondPolicy(),
                new ThirdPolicy()
            });

            multiplePolicies.Whitelist.NameSpaces.ShouldNotBeNull();
            multiplePolicies.MethodValidators.Count.ShouldBe(6);
        }

        [Fact]
        public void Policy_ArrayValidator_Test()
        {
            var validator = new ArrayValidator();
            var validateResult1 = ValidateContractCode(_badContractCode, validator);
            validateResult1.Count.ShouldBeGreaterThan(0);
            validateResult1.First().Message.ShouldContain("Array size is too large");

            var validateResult2 = ValidateContractCode(_systemContractCode, validator);
            validateResult2.Count.ShouldBe(0);
        }

        [Fact]
        public void Policy_FloatOpsValidator_Test()
        {
            var validator = new FloatOpsValidator();
            var validateResult1 = ValidateContractCode(_badContractCode, validator);
            validateResult1.Count.ShouldBeGreaterThan(0);
            validateResult1.First().Message.ShouldContain("contains ldc.r8 float OpCode");

            var validateResult2 = ValidateContractCode(_systemContractCode, validator);
            validateResult2.Count.ShouldBe(0);
        }

        [Fact]
        public void Policy_GenericParamValidator_Test()
        {
            var validator = new GenericParamValidator();
            var validateResult1 = ValidateContractCode(_badContractCode, validator);
            validateResult1.Count.ShouldBe(0); //no error sample

            var validateResult2 = ValidateContractCode(_systemContractCode, validator);
            validateResult2.Count.ShouldBe(0);
        }

        [Fact]
        public void Policy_MultiDimArrayValidator_Test()
        {
            var validator = new MultiDimArrayValidator();
            var validateResult1 = ValidateContractCode(_badContractCode, validator);
            validateResult1.Count.ShouldBe(0); //no error sample

            var validateResult2 = ValidateContractCode(_systemContractCode, validator);
            validateResult2.Count.ShouldBe(0);
        }

        [Fact]
        public void Policy_NewObjValidator_Test()
        {
            var validator = new NewObjValidator();
            var validateResult1 = ValidateContractCode(_badContractCode, validator);
            validateResult1.Count.ShouldBeGreaterThan(0);
            validateResult1.First().Message.ShouldContain("objects is not supported");
        }

        [Fact]
        public void Policy_UncheckedMathValidator_Test()
        {
            var validator = new UncheckedMathValidator();
            var validateResult1 = ValidateContractCode(_badContractCode, validator);
            validateResult1.Count.ShouldBeGreaterThan(0);
            validateResult1.First().Message.ShouldContain("contains unsafe OpCode add");
        }

        [Fact]
        public void ContractAuditor_Basic_Test()
        {
            var whiteList = new List<string>
            {
                "System.Collection",
                "System.Linq"
            };
            var blackList = new List<string>
            {
                "System.Random",
                "System.DateTime"
            };

            _auditor = new ContractAuditor(whiteList, blackList);

            Should.Throw<InvalidCodeException>(() => _auditor.Audit(_badContractCode, true));
        }

        private static List<ValidationResult> ValidateContractCode(byte[] code, IValidator<MethodDefinition> validator)
        {
            var modDef = ModuleDefinition.ReadModule(new MemoryStream(code));
            var validateList = new List<ValidationResult>();
            foreach (var typeInfo in modDef.Types)
            {
                foreach (var method in typeInfo.Methods)
                {
                    var validateResult = validator.Validate(method);
                    var count = validateResult.Count();
                    if (count != 0)
                        validateList.AddRange(validateResult);
                }
            }

            return validateList;
        }

        private static byte[] ReadCode(string path)
        {
            return File.ReadAllBytes(path);
        }
    }
}
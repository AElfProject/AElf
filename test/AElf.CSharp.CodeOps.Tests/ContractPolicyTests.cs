using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.CSharp.CodeOps.Patchers.Module;
using AElf.CSharp.CodeOps.Policies;
using AElf.CSharp.CodeOps.Validators;
using AElf.CSharp.CodeOps.Validators.Assembly;
using AElf.CSharp.CodeOps.Validators.Method;
using AElf.CSharp.CodeOps.Validators.Module;
using AElf.CSharp.CodeOps.Validators.Whitelist;
using AElf.Runtime.CSharp.Tests.BadContract;
using AElf.Runtime.CSharp.Tests.TestContract;
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
        }
    }

    public class ContractPolicyTests : CSharpCodeOpsTestBase
    {
        private ContractAuditor _auditor;
        private readonly byte[] _systemContractCode;
        private readonly byte[] _badContractCode;
        private readonly RequiredAcsDto _requiredAcs;

        public ContractPolicyTests()
        {
            _systemContractCode = ReadPatchedContractCode(typeof(BasicContractZero));
            _badContractCode = ReadContractCode(typeof(BadContract));
            _requiredAcs = new RequiredAcsDto
            {
                AcsList = new[] {"acs1", "acs8"}.ToList(),
                RequireAll = false
            };
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
            multiplePolicies.MethodValidators.Count.ShouldBe(4);
        }

        [Fact]
        public void Policy_ArrayValidator_Test()
        {
            var validator = new ArrayValidator();
            var validateResult1 = ValidateContractCode(_badContractCode, validator);
            validateResult1.Count.ShouldBeGreaterThan(0);
            var messages = validateResult1.Select(res => res.Message).ToArray();
            messages.ShouldContain("Array size is too large that causes overflow when estimating memory usage.");
            messages.ShouldContain("Array of AElf.Runtime.CSharp.Tests.BadContract.BadCase3 type is not allowed.");

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
        public void Policy_MultiDimArrayValidator_Test()
        {
            var validator = new MultiDimArrayValidator();
            var validateResult1 = ValidateContractCode(_badContractCode, validator);
            validateResult1.Count.ShouldBe(0); //no error sample

            var validateResult2 = ValidateContractCode(_systemContractCode, validator);
            validateResult2.Count.ShouldBe(0);
        }

        [Fact]
        public void Policy_UncheckedMathValidator_Test()
        {
            var validator = new UncheckedMathValidator();
            var validateResult1 = ValidateContractCode(ReadContractCode(typeof(TestContract)), validator);
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

            _auditor = new ContractAuditor(blackList, whiteList);

            Should.Throw<InvalidCodeException>(() => _auditor.Audit(_badContractCode, _requiredAcs, true));
        }

        [Fact]
        public void ContractAuditor_AcsRequired_Test()
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

            var requireAcs = new RequiredAcsDto();
            requireAcs.AcsList = new List<string> {"acs1"};
            Should.Throw<InvalidCodeException>(() => _auditor.Audit(_badContractCode, requireAcs, true));

            Should.NotThrow(() => _auditor.Audit(_systemContractCode, requireAcs, true));

            requireAcs.AcsList.Add("acs8");
            Should.NotThrow(() => _auditor.Audit(_systemContractCode, requireAcs, true));

            requireAcs.RequireAll = true;
            Should.Throw<InvalidCodeException>(() => _auditor.Audit(_systemContractCode, requireAcs, true));
        }

        [Fact]
        public void ContractAudit_NotInjectAndCheckObserverProxy_Test()
        {
            var code = ReadCode(typeof(TokenContract).Assembly.Location);
            var changedCode = InjectCallReplacerCode(code);
            var md = ModuleDefinition.ReadModule(new MemoryStream(changedCode));

            var observerValidator = new ObserverProxyValidator();
            var validateResult = observerValidator.Validate(md);
            validateResult.Count().ShouldBeGreaterThan(0);
        }
        
        private static List<ValidationResult> ValidateContractCode(byte[] code, IValidator<MethodDefinition> validator)
        {
            var modDef = ModuleDefinition.ReadModule(new MemoryStream(code));
            var validateList = new List<ValidationResult>();
            foreach (var typeInfo in modDef.Types)
            {
                foreach (var method in typeInfo.Methods)
                {
                    var validateResult = validator.Validate(method).ToList();
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
        
        private static byte[] InjectCallReplacerCode(byte[] code)
        {
            var asm = AssemblyDefinition.ReadAssembly(new MemoryStream(code));
            var patcher = new MethodCallReplacer();
            patcher.Patch(asm.MainModule);

            var newCode = new MemoryStream();
            asm.Write(newCode);
            return newCode.ToArray();
        }
    }
}
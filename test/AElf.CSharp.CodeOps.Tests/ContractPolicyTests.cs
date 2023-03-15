using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.CSharp.CodeOps.Patchers.Module;
using AElf.CSharp.CodeOps.Patchers.Module.SafeMethods;
using AElf.CSharp.CodeOps.Validators;
using AElf.CSharp.CodeOps.Validators.Method;
using AElf.CSharp.CodeOps.Validators.Module;
using AElf.Runtime.CSharp.Tests.BadContract;
using AElf.Runtime.CSharp.Tests.TestContract;
using Mono.Cecil;
using Shouldly;
using Xunit;

namespace AElf.CSharp.CodeOps;

public class ContractPolicyTests : CSharpCodeOpsTestBase
{
    private readonly byte[] _badContractCode;
    private readonly byte[] _systemContractCode;
    private CSharpContractAuditor _auditor;

    public ContractPolicyTests()
    {
        _systemContractCode = ReadPatchedContractCode(typeof(BasicContractZero));
        _badContractCode = ReadContractCode(typeof(BadContract));
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
        validateResult1.Count.ShouldBe(1);

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
    public void ContractAudit_NotInjectAndCheckObserverProxy_Test()
    {
        var code = ReadCode(typeof(TokenContract).Assembly.Location);
        var changedCode = InjectCallReplacerCode(code);
        var md = ModuleDefinition.ReadModule(new MemoryStream(changedCode));

        var observerValidator = new ObserverProxyValidator();
        var validateResult = observerValidator.Validate(md, new CancellationToken());
        validateResult.Count().ShouldBeGreaterThan(0);
    }

    private static List<ValidationResult> ValidateContractCode(byte[] code, IValidator<MethodDefinition> validator)
    {
        var modDef = ModuleDefinition.ReadModule(new MemoryStream(code));
        var validateList = new List<ValidationResult>();
        foreach (var typeInfo in modDef.Types)
        foreach (var method in typeInfo.Methods)
        {
            var validateResult = validator.Validate(method, new CancellationToken()).ToList();
            var count = validateResult.Count();
            if (count != 0)
                validateList.AddRange(validateResult);
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
        var patcher = new StringMethodsReplacer();
        patcher.Patch(asm.MainModule);

        var newCode = new MemoryStream();
        asm.Write(newCode);
        return newCode.ToArray();
    }
}
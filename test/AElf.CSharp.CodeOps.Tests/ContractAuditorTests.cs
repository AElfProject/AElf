using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AElf.Contracts.Association;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Economic;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.Referendum;
using AElf.Contracts.TestContract.TransactionFees;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.CSharp.CodeOps.Validators;
using AElf.CSharp.CodeOps.Validators.Assembly;
using AElf.CSharp.CodeOps.Validators.Method;
using AElf.CSharp.CodeOps.Validators.Module;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Runtime.CSharp.Tests.BadContract;
using Microsoft.Extensions.Options;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Shouldly;
using Xunit;

namespace AElf.CSharp.CodeOps;

public class ContractAuditor : IDisposable
{
    private readonly RequiredAcs _requiredAcs;
    private CSharpContractAuditor _auditor;

    public ContractAuditor(CSharpContractAuditor contractAuditor)
    {
        _auditor = contractAuditor;
        _requiredAcs = new RequiredAcs
        {
            AcsList = new[] { "acs1", "acs8" }.ToList(),
            RequireAll = false
        };
    }

    public void Dispose()
    {
        _auditor = null;
    }

    public void Audit(byte[] code, bool isSystemContract)
    {
        _auditor.Audit(code, _requiredAcs, isSystemContract);
    }

    public void Audit(byte[] code, RequiredAcs requiredAcs, bool isSystemContract)
    {
        _auditor.Audit(code, requiredAcs, isSystemContract);
    }
}

public class ContractAuditorTests : CSharpCodeOpsTestBase
{
    private readonly ContractAuditor _auditor;
    private readonly IContractPatcher _patcher;

    public ContractAuditorTests()
    {
        // Use fixture to instantiate auditor only once
        _auditor = GetRequiredService<ContractAuditor>();
        _patcher = GetRequiredService<IContractPatcher>();
    }

    #region Positive Cases

    [Theory]
    [InlineData(typeof(AssociationContract), false, "acs3", "acs5")]
    [InlineData(typeof(ConfigurationContract))]
    [InlineData(typeof(AEDPoSContract), true, "acs1", "acs6", "acs10")]
    [InlineData(typeof(CrossChainContract), true, "acs6")]
    [InlineData(typeof(EconomicContract))]
    [InlineData(typeof(ElectionContract))]
    [InlineData(typeof(BasicContractZero))]
    [InlineData(typeof(TokenContract))]
    [InlineData(typeof(ParliamentContract))]
    [InlineData(typeof(ProfitContract))]
    [InlineData(typeof(ReferendumContract))]
    [InlineData(typeof(TokenConverterContract))]
    [InlineData(typeof(TreasuryContract))]
    public void CheckSystemContracts_AllShouldPass(Type contractType, bool acsAllRequired = false,
        params string[] acsList)
    {
        var requiredAcs = new RequiredAcs
        {
            RequireAll = acsAllRequired,
            AcsList = new List<string>(acsList)
        };
        _auditor.Audit(ReadPatchedContractCode(contractType), true);
        Should.Throw<CSharpCodeCheckException>(() =>
            _auditor.Audit(ReadPatchedContractCode(contractType), requiredAcs, false));
    }

    [Fact]
    public void AuditTimeout()
    {
        Should.NotThrow(() => _auditor.Audit(ReadPatchedContractCode(typeof(TokenContract)), true));
        var codeOpsOptions = GetRequiredService<IOptionsMonitor<CSharpCodeOpsOptions>>();
        codeOpsOptions.CurrentValue.AuditTimeoutDuration = 0;
        Should.Throw<ContractAuditTimeoutException>(() =>
            _auditor.Audit(ReadPatchedContractCode(typeof(TokenContract)), true));
    }

    [Fact]
    public void ContractPatcher_Test()
    {
        var code = ReadContractCode(typeof(TokenContract));
        var updateCode = _patcher.Patch(code, false);
        code.ShouldNotBe(updateCode);
        var exception = Record.Exception(() => _auditor.Audit(updateCode, true));
        exception.ShouldBeNull();
    }

    [Fact]
    public void ContractAudit_SystemPolicy_Test()
    {
        var code = ReadPatchedContractCode(typeof(TokenContract));

        Should.NotThrow(() => _auditor.Audit(code, true));

        Should.Throw<CSharpCodeCheckException>(() => _auditor.Audit(code, false))
            .Findings.Count(f => f is ObserverProxyValidationResult)
            .ShouldBeGreaterThan(0);
    }

    #endregion

    #region Negative Cases

    [Fact]
    public void CheckBadContract_ForFindings()
    {
        var findings = Should.Throw<CSharpCodeCheckException>(
                () => _auditor.Audit(ReadContractCode(typeof(BadContract)), false))
            .Findings;

        // Should have identified that ACS1 or ACS8 is not there
        findings.FirstOrDefault(f => f is AcsValidationResult).ShouldNotBeNull();

        // Random usage
        LookFor(findings,
                "UpdateStateWithRandom",
                i => i.Namespace == "System" && i.Type == "Random")
            .ShouldNotBeNull();

        // DateTime UtcNow usage
        LookFor(findings,
                "UpdateStateWithCurrentTime",
                i => i.Namespace == "System" && i.Type == "DateTime" && i.Member == "get_UtcNow")
            .ShouldNotBeNull();

        // DateTime Now usage
        LookFor(findings,
                "UpdateStateWithCurrentTime",
                i => i.Namespace == "System" && i.Type == "DateTime" && i.Member == "get_Now")
            .ShouldNotBeNull();

        // DateTime Today usage
        LookFor(findings,
                "UpdateStateWithCurrentTime",
                i => i.Namespace == "System" && i.Type == "DateTime" && i.Member == "get_Today")
            .ShouldNotBeNull();

        // Double type usage
        LookFor(findings,
                "UpdateDoubleState",
                i => i.Namespace == "System" && i.Type == "Double")
            .ShouldNotBeNull();

        // Float type usage
        LookFor(findings,
                "UpdateFloatState",
                i => i.Namespace == "System" && i.Type == "Single")
            .ShouldNotBeNull();

        // Disk Ops usage
        LookFor(findings,
                "WriteFileToNode",
                i => i.Namespace == "System.IO")
            .ShouldNotBeNull();

        // String constructor usage
        LookFor(findings,
                "InitLargeStringDynamic",
                i => i.Namespace == "System" && i.Type == "String" && i.Member == ".ctor")
            .ShouldNotBeNull();

        // Denied member use in separate class
        LookFor(findings,
                "UseDeniedMemberInSeparateClass",
                i => i.Namespace == "System" && i.Type == "DateTime" && i.Member == "get_Now")
            .ShouldNotBeNull();

        // Large array initialization
        findings.FirstOrDefault(f => f is ArrayValidationResult && f.Info.ReferencingMethod == "InitLargeArray")
            .ShouldNotBeNull();

        // Float operations
        findings.FirstOrDefault(f => f is FloatOpsValidationResult)
            .ShouldNotBeNull();

        var getHashCodeFindings = findings.Where(f => f is GetHashCodeValidationResult).ToList();
        // getHashCodeFindings.FirstOrDefault(f => f.Info.Type == nameof(BadContract)).ShouldNotBeNull();
        // getHashCodeFindings.FirstOrDefault(f => f.Info.Type == nameof(DoubleInput)).ShouldNotBeNull();
        // getHashCodeFindings.FirstOrDefault(f => f.Info.Type == nameof(FloatInput)).ShouldNotBeNull();
        // getHashCodeFindings.FirstOrDefault(f => f.Info.Type == nameof(IMessageInheritedClass)).ShouldNotBeNull();

        LookFor(getHashCodeFindings, "TestGetHashCodeCall", f => f != null).ShouldNotBeNull();
        LookFor(getHashCodeFindings, "GetHashCode", f => f != null).ShouldNotBeNull();

        // FileDescriptor type field not allowed to be set outside of its declaring type constructor
        findings
            .FirstOrDefault(f => f is DescriptorAccessValidationResult &&
                                 f.Info.Type == "BadCase1" && f.Info.ReferencingMethod == "SetFileDescriptor")
            .ShouldNotBeNull();

        // Initialization value not allowed for resettable members, in regular type
        findings
            .FirstOrDefault(f => f is ContractStructureValidationResult &&
                                 f.Info.Type == "BadCase2" && f.Info.ReferencingMethod == ".cctor" &&
                                 f.Info.Member == "Number").ShouldNotBeNull();

        // findings.FirstOrDefault(f => f is ContractStructureValidationResult &&
        //                              f.Info.Type == "BadContract" && f.Info.Member == "staticNotAllowedTypeField").ShouldBeNull();
        // findings.FirstOrDefault(f => f is ContractStructureValidationResult &&
        //                              f.Info.Type == "BadContract" && f.Info.Member == "staticAllowedTypeField").ShouldBeNull();
        //
        // A type that is not allowed as a static readonly field
        findings.FirstOrDefault(f => f is ContractStructureValidationResult &&
                                     f.Info.Type == "BadCase3" && f.Info.Member == "field").ShouldNotBeNull();

        // Not allowed as readonly if a type has a field with its own type and has instance field
        findings.FirstOrDefault(f => f is ContractStructureValidationResult &&
                                     f.Info.Type == "BadCase4" && f.Info.Member == "field").ShouldNotBeNull();

        // Non primitive type is not allowed as an argument in ReadOnly GenericInstanceType type in static readonly fields
        findings.FirstOrDefault(f => f is ContractStructureValidationResult &&
                                     f.Info.Type == "BadCase5" && f.Info.Member == "collection").ShouldNotBeNull();

        // A type that is not allowed to be used as a field in contract state
        findings
            .FirstOrDefault(f => f is ContractStructureValidationResult &&
                                 f.Info.Type == "BadContractState" && f.Info.Member == "i").ShouldNotBeNull();
    }

    [Fact]
    public void CheckContractStruct_ForComponentsMissing()
    {
        {
            var module = GetModule(typeof(ArrayValidator));
            var contractStructure = new ContractStructureValidator();
            var validationResults = contractStructure.Validate(module, CancellationToken.None);
            validationResults
                .FirstOrDefault(f =>
                    f is ContractStructureValidationResult && f.Message == "Contract base not found.")
                .ShouldNotBeNull();
        }
    }

    [Fact(Skip = "Static field not allowed in user code https://github.com/AElfProject/AElf/issues/3388")]
    public void CheckResetField_ForMissingReset()
    {
        var code = ReadContractCode(typeof(BadContract));
        var module = ModuleDefinition.ReadModule(new MemoryStream(code));
        var resetFieldsMethod = module.Types
            .Single(t => t.IsContractImplementation())
            .Methods.Single(m => m.Name == "ResetFields");

        RemoveInstruction(resetFieldsMethod, i => i.OpCode == OpCodes.Stfld); // Remove field reset
        RemoveInstruction(resetFieldsMethod, i =>
            i.OpCode == OpCodes.Call && // Remove call to other type's ResetFields
            i.Operand is MethodDefinition method &&
            method.Name == "ResetFields");

        var instruction = ReplaceInstruction(resetFieldsMethod, OpCodes.Ldc_I4_1, i => i.OpCode == OpCodes.Ldc_I4_0);

        var tamperedContract = new MemoryStream();
        module.Write(tamperedContract);

        var findings = Should.Throw<CSharpCodeCheckException>(
                () => _auditor.Audit(tamperedContract.ToArray(), false))
            .Findings;

        findings.FirstOrDefault(f => f is ResetFieldsValidationResult &&
                                     f.Message.Contains("missing reset for certain fields")).ShouldNotBeNull();

        findings.FirstOrDefault(f => f is ResetFieldsValidationResult &&
                                     f.Message.Contains("missing certain method calls")).ShouldNotBeNull();

        findings.FirstOrDefault(f => f is ResetFieldsValidationResult &&
                                     f.Message.Contains(
                                         $"Unexpected instruction {instruction} found in ResetFields method."))
            .ShouldNotBeNull();
    }

    [Fact]
    public void CheckPatchAudit_ForUncheckedMathOpcodes()
    {
        // Here, we use any contract that contains unchecked math OpCode even with "Check for arithmetic overflow"
        // checked in the project. If first section of below test case fails, need to create another contract  
        // that iterates an array with foreach loop.
        var contractCode = ReadContractCode(typeof(TransactionFeesContract));

        var findings = Should.Throw<CSharpCodeCheckException>(
                () => _auditor.Audit(contractCode, false))
            .Findings;

        findings.FirstOrDefault(f => f is UncheckedMathValidationResult)
            .ShouldNotBeNull();

        // After patching, all unchecked arithmetic OpCodes should be cleared.
        Should.NotThrow(() => _auditor.Audit(_patcher.Patch(contractCode, false), false));
    }

    [Fact]
    public void CheckPatchAudit_ForMethodCallInjection()
    {
        var contractCode = ReadContractCode(typeof(TransactionFeesContract));

        var findings = Should.Throw<CSharpCodeCheckException>(
                () => _auditor.Audit(contractCode, false))
            .Findings;

        findings.Count(f => f is MethodCallInjectionValidationResult).ShouldBe(3);
        findings.Count(f => f is ObserverProxyValidationResult).ShouldBe(1);

        // After patching, all unchecked arithmetic OpCodes should be cleared.
        Should.NotThrow(() => _auditor.Audit(_patcher.Patch(contractCode, false), false));

        var badContractModule = GetModule(typeof(BadContract));
        var validator = new ObserverProxyValidator();
        var validationResults = validator.Validate(badContractModule, CancellationToken.None);
        validationResults.ShouldContain(v => v.Message.Contains("BranchCount proxy method body is tampered."));
        validationResults.ShouldContain(v => v.Message.Contains("CallCount proxy method body is tampered."));
        validationResults.ShouldContain(v =>
            v.Message.Contains("Missing execution observer call count call detected."));
        validationResults.ShouldContain(v =>
            v.Message.Contains("Missing execution observer branch count call detected."));
        validationResults.ShouldContain(v =>
            v.Message.Contains("Proxy initialize call detected from within the contract."));
    }

    [Fact]
    public void ContractAuditor_AcsRequired_Test()
    {
        var requireAcs = new RequiredAcs();
        requireAcs.AcsList = new List<string> { "acs1" };
        var badContractCode = ReadContractCode(typeof(BadContract));
        Should.Throw<CSharpCodeCheckException>(() => _auditor.Audit(badContractCode, requireAcs, false));

        var systemContractCode = ReadPatchedContractCode(typeof(BasicContractZero));

        Should.NotThrow(() => _auditor.Audit(systemContractCode, requireAcs, true));

        requireAcs.AcsList.Add("acs8");
        Should.NotThrow(() => _auditor.Audit(systemContractCode, requireAcs, true));

        requireAcs.RequireAll = true;
        Should.Throw<CSharpCodeCheckException>(() => _auditor.Audit(systemContractCode, requireAcs, true));
    }

    #endregion

    #region Test Helpers

    private Info LookFor(IEnumerable<ValidationResult> findings, string referencingMethod, Func<Info, bool> criteria)
    {
        return findings.Select(f => f.Info)
            .FirstOrDefault(i => i != null && i.ReferencingMethod == referencingMethod && criteria(i));
    }


    private void RemoveInstruction(MethodDefinition method, Func<Mono.Cecil.Cil.Instruction, bool> where)
    {
        var il = method.Body.GetILProcessor();

        il.Remove(method.Body.Instructions.First(where));
    }

    private Mono.Cecil.Cil.Instruction ReplaceInstruction(MethodDefinition method, OpCode opCode,
        Func<Mono.Cecil.Cil.Instruction, bool> where)
    {
        var il = method.Body.GetILProcessor();

        var instruction = il.Create(opCode);
        il.Replace(method.Body.Instructions.First(where), instruction);
        return instruction;
    }

    #endregion
}